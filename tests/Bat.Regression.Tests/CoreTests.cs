using System.Globalization;

namespace Bat.Regression.Tests;

public class Sample
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; }
    public List<string> Tags { get; set; }
    public Sample Child { get; set; }
    public int Field;
    public string Number { get; set; }
}

public class SerializationTests
{
    [Fact]
    public void DefaultOptions_AreShared_AndReadOnly()
    {
        Assert.Same(SerializationExtension.DefaultOptions, SerializationExtension.DefaultOptions);
        Assert.Same(SerializationExtension.GetSharedOption(3), SerializationExtension.GetSharedOption(3));
        Assert.Throws<InvalidOperationException>(() => SerializationExtension.DefaultOptions.WriteIndented = true);
    }

    [Fact]
    public void GetDefaultOption_StillReturnsNewMutableInstance()
    {
        var a = SerializationExtension.GetDefaultOption();
        a.WriteIndented = true;
        Assert.NotSame(a, SerializationExtension.GetDefaultOption());
    }

    [Fact]
    public void Serialize_UsesBatConventions()
    {
        var json = new Sample { Id = 5, Name = "<b>", Field = 7 }.SerializeToJson();
        Assert.Contains("\"id\":5", json);          // camelCase
        Assert.Contains("\"field\":7", json);       // IncludeFields
        Assert.Contains("\"name\":\"<b>\"", json);  // relaxed escaping
        Assert.Equal(string.Empty, ((object)null).SerializeToJson());
    }

    [Fact]
    public void Deserialize_IsLenient_AndCultureInvariant()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
        try
        {
            var s = "{\"ID\":\"9\",\"active\":\"true\",\"number\":-12.75,}".DeSerializeJson<Sample>();
            Assert.Equal(9, s.Id);
            Assert.True(s.Active);
            Assert.Equal("-12.75", s.Number);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Utf8Bytes_MatchStringSerialization()
    {
        var sample = new Sample { Id = 1, Name = "مهران" };
        Assert.Equal(sample.SerializeToJson(), System.Text.Encoding.UTF8.GetString(sample.SerializeToJsonUtf8Bytes()));
        Assert.Equal("مهران", sample.SerializeToJsonUtf8Bytes().DeSerializeJson<Sample>().Name);
        Assert.Null(Array.Empty<byte>().DeSerializeJson<Sample>());
    }
}

public class CryptoTests
{
    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("سلام دنیا 123")]
    public void Encrypt_Decrypt_RoundTrip(string text)
    {
        Assert.Equal(text, AesEncryption.Decrypt(AesEncryption.Encrypt(text)));
        Assert.Equal(text, Encryption.Decrypt(Encryption.Encrypt(text)));
        // Rijndael (128-bit block) and AES are the same algorithm: outputs must be identical.
        Assert.Equal(AesEncryption.Encrypt(text), Encryption.Encrypt(text));
    }

    [Fact]
    public void Ciphertexts_FromBat_10_0_0_StillMatch()
    {
        // Values produced by the original stream-based implementation (Bat 10.0.0). Data already stored in
        // databases (EF value converters) depends on these never changing.
        Assert.Equal("wNrN1Lyu+nG/jpihCHhmBg==", Encryption.Encrypt("hello"));
        Assert.Equal("wNrN1Lyu+nG/jpihCHhmBg==", AesEncryption.Encrypt("hello"));
        Assert.Equal("hello", AesEncryption.Decrypt("wNrN1Lyu+nG/jpihCHhmBg=="));
        Assert.Equal("jWXI4GuVvusD/euQHIxiqw==", AesAlgorithm.Encrypt("سلام 1", "key1", "salt12", "SHA1", 2, "XYZxyzAZSawsTRCE", 256));
        Assert.Equal("سلام 1", AesAlgorithm.Decrypt("jWXI4GuVvusD/euQHIxiqw==", "key1", "salt12", "SHA1", 2, "XYZxyzAZSawsTRCE", 256));
    }
}

public class ValidatorTests
{
    [Fact]
    public void NationalCode_ChecksChecksum()
    {
        Assert.True("0084575948".IsNationalCode());
        Assert.False("0084575949".IsNationalCode());
        Assert.Equal("0084575948".IsNationalCode(), "0084575948".IsNationalCode2());
    }

    [Fact]
    public void Validators_Work()
    {
        Assert.True("09121234567".IsMobileNumber());
        Assert.True(MobileNumberExtensions.IsMciMobileNumber("09121234567"));
        Assert.True("a.b@test.com".IsEmail());
        Assert.True("192.168.1.1".IsIp());
        Assert.False("999.1.1.1".IsIp());
        Assert.True("12:30".IsTime());
        Assert.True("1403/01/15".IsPersianDate());
        Assert.True(new PersianDateAttribute().IsValid("1403-1-5"));
        Assert.False("2024/01/15".IsPersianDate());
        Assert.True("a.JPG".IsPicture());
        Assert.False("a.txt".IsPicture());
        Assert.True("Abcdef1!".IsComplexPassword());
        Assert.Equal(9121234567, "989121234567".ToStandardMobileNumber());
        Assert.True(new BiggerThanZero().IsValid(null));
    }
}

public class MiscCoreTests
{
    [Fact]
    public void PersianDateTime_Now_IsTehranTime_WithoutDst()
    {
        var tehran = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
        Assert.True(Math.Abs((PersianDateTime.Now.ToDateTime() - tehran).TotalMinutes) < 1);
        Assert.Equal("فروردین", PersianDateTime.GetMonthName(1));
        Assert.Equal("اسفند", PersianDateTime.GetMonthName(12));
    }

    [Fact]
    public void Randomizer_UsesWholeAlphabet()
    {
        var r = Randomizer.GetRandomString(200);
        Assert.Equal(200, r.Length);
        Assert.True(r.Distinct().Count() > 20);
        Assert.Equal(5, Randomizer.GetRandomInteger(5).ToString().Length);
    }

    [Fact]
    public void Misc_Fixes()
    {
        Assert.False(Guid.Empty.IsNotNull());
        var sample = new Sample { Name = "x", Id = 3 };
        Assert.Equal("x", ((object)sample).GetProperty("Name"));
        var copy = new Sample().CopyFrom(sample);
        Assert.Equal(3, copy.Id);
        Assert.Equal("123abc4", "۱۲۳abc٤".ToEnglishNumber());
        Assert.True(FileOperation.CheckExtension(FileType.Image, "x.PNG"));
        Assert.True(FileOperation.CheckExtension(FileType.Image, "x.heic"));
        Assert.True("x.HEIC".IsPicture());
#pragma warning disable CS0618 // old names must keep working for existing consumers
        Assert.True(FileOperation.CheckExtention(FileType.Image, "x.png"));
        Assert.Equal(RegexPattern.IranCellMobileNumber, RegexPattern.IrancellMobileNumber);
#pragma warning restore CS0618
        Assert.True(MobileNumberExtensions.IsIrancellMobileNumber("09351234567"));
        Assert.Equal(2, new[] { 1, 2, 3 }.Select(x => x).ToPagingListModel(new PagingParameter(1, 2)).TotalPages);
    }

    [Fact]
    public void MenuModel_ChildMenus_CachedPerValue()
    {
        var menu = new MenuModel { Menus = "[{\"menuId\":2}]" };
        Assert.Same(menu.ChildMenus, menu.ChildMenus);
        menu.Menus = "[{\"menuId\":3},{\"menuId\":4}]";
        Assert.Equal(2, menu.ChildMenus.Count);
    }

    [Fact]
    public void FileLoger_WritesOneFilePerDay_WithValidName()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bat-tests-log-" + Guid.NewGuid());
        FileLoger.Info("a", dir);
        FileLoger.Info("b", dir);
        var file = Assert.Single(Directory.GetFiles(dir));
        Assert.DoesNotContain(':', Path.GetFileName(file));
    }
}
