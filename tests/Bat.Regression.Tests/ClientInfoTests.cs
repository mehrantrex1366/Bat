using Bat.Http;
using Microsoft.AspNetCore.Http;

namespace Bat.Regression.Tests;

public class ClientInfoTests
{
    // User agent | OS | OS version | browser | browser version | device type
    public static TheoryData<string, string, string, string, string, string> UserAgents => new()
    {
        // Windows
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Windows 10", "", "Chrome", "124.0.0.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.2478.67", "Windows 10", "", "Edge", "124.0.2478.67", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36 Edge/18.19582", "Windows 10", "", "Edge", "18.19582", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 OPR/110.0.0.0", "Windows 10", "", "Opera", "110.0.0.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0", "Windows 10", "", "Firefox", "125.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 YaBrowser/24.4.0.0 Safari/537.36", "Windows 10", "", "Yandex", "24.4.0.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Vivaldi/6.7.3329.21", "Windows 10", "", "Vivaldi", "6.7.3329.21", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko", "Windows 10", "", "Internet Explorer", "11.0", "Desktop" },
        { "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)", "Windows 8", "", "Internet Explorer", "10.0", "Desktop" },
        { "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)", "Windows 7", "", "Internet Explorer", "8.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36", "Windows 8.1", "", "Chrome", "109.0.0.0", "Desktop" },
        { "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1)", "Windows XP", "", "Internet Explorer", "6.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.112 Safari/537.36", "Windows Vista", "", "Chrome", "49.0.2623.112", "Desktop" },
        { "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.22621.2506", "Windows 10", "", "PowerShell", "5.1.22621.2506", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Slack/4.36.140 Chrome/120.0.6099.291 Electron/28.2.0 Safari/537.36", "Windows 10", "", "Electron", "28.2.0", "Desktop" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/124.0.6367.60 Safari/537.36", "Windows 10", "", "Chrome Headless", "124.0.6367.60", "Desktop" },

        // macOS
        { "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15", "Mac OS", "10.15.7", "Safari", "17.4.1", "Desktop" },
        { "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Mac OS", "10.15.7", "Chrome", "124.0.0.0", "Desktop" },
        { "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:125.0) Gecko/20100101 Firefox/125.0", "Mac OS", "10.15", "Firefox", "125.0", "Desktop" },

        // Linux / Chrome OS / BSD
        { "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Linux", "", "Chrome", "124.0.0.0", "Desktop" },
        { "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:125.0) Gecko/20100101 Firefox/125.0", "Ubuntu", "", "Firefox", "125.0", "Desktop" },
        { "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:124.0) Gecko/20100101 Firefox/124.0", "Fedora", "", "Firefox", "124.0", "Desktop" },
        { "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Chrome OS", "14541.0.0", "Chrome", "124.0.0.0", "Desktop" },
        { "Mozilla/5.0 (X11; FreeBSD amd64; rv:122.0) Gecko/20100101 Firefox/122.0", "FreeBSD", "", "Firefox", "122.0", "Desktop" },
        { "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15 Epiphany/46.0", "Linux", "", "GNOME Web", "46.0", "Desktop" },

        // Android
        { "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36", "Android", "10", "Chrome", "124.0.0.0", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 13; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36", "Android", "13", "Chrome", "112.0.0.0", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 13; SAMSUNG SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/23.0 Chrome/115.0.0.0 Mobile Safari/537.36", "Android", "13", "SamsungBrowser", "23.0", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36 EdgA/124.0.2478.64", "Android", "10", "Edge", "124.0.2478.64", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36 OPR/81.1.4292.78446", "Android", "10", "Opera", "81.1.4292.78446", "Mobile" },
        { "Mozilla/5.0 (Android 14; Mobile; rv:125.0) Gecko/125.0 Firefox/125.0", "Android", "14", "Firefox", "125.0", "Mobile" },
        { "Mozilla/5.0 (Android 14; Tablet; rv:125.0) Gecko/125.0 Firefox/125.0", "Android", "14", "Firefox", "125.0", "Tablet" },
        { "Mozilla/5.0 (Linux; Android 11; Pixel 5 Build/RQ3A.210805.001.A1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/94.0.4606.71 Mobile Safari/537.36", "Android", "11", "Chrome WebView", "94.0.4606.71", "Mobile" },
        { "Mozilla/5.0 (Linux; U; Android 12; zh-cn; 2201123C Build/SKQ1.211006.001) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/100.0.4896.127 Mobile Safari/537.36 XiaoMi/MiuiBrowser/17.3.100105", "Android", "12", "MIUI Browser", "17.3.100105", "Mobile" },
        { "Mozilla/5.0 (Linux; U; Android 4.0.3; ko-kr; LG-L160L Build/IML74K) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30", "Android", "4.0.3", "Android Browser", "4.0", "Mobile" },
        { "Mozilla/5.0 (Linux; U; Android 8.1.0; en-US; Nexus 6P Build/OPM7.181205.001) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/12.11.1.1197 Mobile Safari/537.36", "Android", "8.1.0", "UCBrowser", "12.11.1.1197", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 13; SM-X706B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Android", "13", "Chrome", "124.0.0.0", "Tablet" },
        { "Mozilla/5.0 (Linux; Android 12; HarmonyOS; ANA-AN00; HMSCore 6.6.0.352) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.88 HuaweiBrowser/14.0.5.302 Mobile Safari/537.36", "HarmonyOS", "", "Huawei Browser", "14.0.5.302", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 9; KFMAWI) AppleWebKit/537.36 (KHTML, like Gecko) Silk/124.2.1 like Chrome/124.0.6367.82 Safari/537.36", "Fire OS", "", "Silk", "124.2.1", "Tablet" },
        { "Mozilla/5.0 (Linux; Android 13; SM-S911B Build/TP1A.220624.014; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/124.0.6367.82 Mobile Safari/537.36 Instagram 330.0.0.40.92 Android (33/13; 420dpi; 1080x2340; samsung; SM-S911B; dm1q; qcom; en_US; 597378196)", "Android", "13", "Instagram", "330.0.0.40.92", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 13; SM-S911B Build/TP1A.220624.014; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/124.0.6367.82 Mobile Safari/537.36 [FB_IAB/FB4A;FBAV/462.0.0.47.86;]", "Android", "13", "Facebook", "462.0.0.47.86", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Focus/124.0 Chrome/124.0.6367.82 Mobile Safari/537.36", "Android", "10", "Firefox Focus", "124.0", "Mobile" },
        { "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile DuckDuckGo/5 Safari/537.36", "Android", "10", "DuckDuckGo", "5", "Mobile" },
        { "Dalvik/2.1.0 (Linux; U; Android 11; SM-A515F Build/RP1A.200720.012)", "Android", "11", "Dalvik", "2.1.0", "Mobile" },

        // iOS / iPadOS
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Mobile/15E148 Safari/604.1", "IOS", "17.4.1", "Safari", "17.4.1", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Mobile/15E148 Safari/604.1", "IOS", "26.0", "Safari", "26.0", "Mobile" },
        { "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1", "IOS", "17.4", "Safari", "17.4", "Tablet" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/124.0.6367.88 Mobile/15E148 Safari/604.1", "IOS", "17.4", "Chrome", "124.0.6367.88", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/125.0 Mobile/15E148 Safari/605.1.15", "IOS", "17.4", "Firefox", "125.0", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 EdgiOS/124.2478.71 Mobile/15E148 Safari/605.1.15", "IOS", "17.4", "Edge", "124.2478.71", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148", "IOS", "17.4", "iOS WebView", "", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) GSA/312.0.623722638 Mobile/15E148 Safari/604.1", "IOS", "17.4", "Google App", "312.0.623722638", "Mobile" },
        { "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 330.0.3.25.90 (iPhone14,5; iOS 17_4; en_US; en; scale=3.00; 1170x2532; 597000000)", "IOS", "17.4", "Instagram", "330.0.3.25.90", "Mobile" },
        { "Mozilla/5.0 (iPod touch; CPU iPhone OS 12_5_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1.2 Mobile/15E148 Safari/604.1", "IOS", "12.5.7", "Safari", "12.1.2", "Mobile" },

        // Other mobile systems
        { "Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.15063", "Windows Phone", "10.0", "Edge", "15.15063", "Mobile" },
        { "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 920)", "Windows Phone", "8.0", "IE Mobile", "10.0", "Mobile" },
        { "Mozilla/5.0 (BB10; Touch) AppleWebKit/537.10+ (KHTML, like Gecko) Version/10.0.9.2372 Mobile Safari/537.10+", "Black Berry", "10.0.9.2372", "Safari", "10.0.9.2372", "Mobile" },
        { "Mozilla/5.0 (Mobile; Nokia_8110_4G; rv:48.0) Gecko/48.0 Firefox/48.0 KAIOS/2.5", "KaiOS", "2.5", "Firefox", "48.0", "Mobile" },
        { "Opera/9.80 (J2ME/MIDP; Opera Mini/9.80 (S60; SymbOS; Opera Mobi/23.348; U; en) Presto/2.5.25 Version/10.54", "Symbian", "", "Opera Mini", "9.80", "Mobile" },

        // TV / console / wearable
        { "Mozilla/5.0 (SMART-TV; Linux; Tizen 6.0) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/4.0 Chrome/76.0.3809.146 TV Safari/537.36", "Tizen", "6.0", "SamsungBrowser", "4.0", "TV" },
        { "Mozilla/5.0 (Web0S; Linux/SmartTV) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.79 Safari/537.36 WebAppManager", "webOS", "", "Chrome", "79.0.3945.79", "TV" },
        { "Mozilla/5.0 (Linux; Android 9; AFTMM Build/PS7633) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.230 Mobile Safari/537.36", "Fire OS", "", "Chrome", "120.0.6099.230", "TV" },
        { "Mozilla/5.0 (X11; Linux armv7l) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.152 Safari/537.36 CrKey/1.56.500000 DeviceType/Chromecast", "Chromecast", "1.56.500000", "Chrome", "88.0.4324.152", "TV" },
        { "Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.2478.67", "Xbox", "", "Edge", "124.0.2478.67", "Console" },
        { "Mozilla/5.0 (PlayStation; PlayStation 5/2.26) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Safari/605.1.15", "PlayStation", "", "Safari", "13.0", "Console" },
        { "Mozilla/5.0 (Nintendo Switch; WifiWebAuthApplet) AppleWebKit/606.4 (KHTML, like Gecko) NF/6.0.1.15.4 NintendoBrowser/5.1.0.20393", "Nintendo", "", "Unknown", "0.0", "Console" },

        // Bots, tools and probes
        { "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", "Unknown", "", "Googlebot", "2.1", "Bot" },
        { "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.6367.60 Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", "Android", "6.0.1", "Googlebot", "2.1", "Bot" },
        { "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)", "Unknown", "", "Bingbot", "2.0", "Bot" },
        { "facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)", "Unknown", "", "Facebook Crawler", "1.1", "Bot" },
        { "Mozilla/5.0 (compatible; MJ12bot/v1.4.8; http://mj12bot.com/)", "Unknown", "", "MJ12bot", "1.4.8", "Bot" },
        { "Mozilla/5.0 (compatible; SomeNewCrawler/3.1; +https://example.com/crawler)", "Unknown", "", "SomeNewCrawler", "3.1", "Bot" },
        { "kube-probe/1.29", "Unknown", "", "kube-probe", "1.29", "Bot" },
        { "PostmanRuntime/7.37.3", "Postman", "7.37.3", "Postman", "7.37.3", "Unknown" },
        { "curl/8.7.1", "Unknown", "", "curl", "8.7.1", "Unknown" },
        { "python-requests/2.31.0", "Unknown", "", "Python Requests", "2.31.0", "Unknown" },
        { "okhttp/4.12.0", "Unknown", "", "OkHttp", "4.12.0", "Unknown" },
        { "", "Unknown", "", "Unknown", "0.0", "Unknown" },
    };

    [Theory]
    [MemberData(nameof(UserAgents))]
    public void ParseUserAgent_DetectsOsBrowserAndDevice(string userAgent, string os, string osVersion, string browser, string browserVersion, string deviceType)
    {
        var details = ClientInfo.ParseUserAgent(userAgent);

        Assert.Equal(os, details.OsName);
        Assert.Equal(osVersion, details.OsVersion);
        Assert.Equal(browser, details.BrowserName);
        Assert.Equal(browserVersion, details.BrowserVersion);
        Assert.Equal(deviceType, details.DeviceType);
        Assert.Equal(deviceType is "Mobile" or "Tablet" or "Wearable", details.IsMobile);
        Assert.Equal(deviceType == "Tablet", details.IsTablet);
        Assert.Equal(deviceType == "Bot", details.IsBot);

        // The old out-parameter API must agree with the new parser.
        ClientInfo.GetRequestOs(userAgent, out var oldOs, out var oldOsVersion);
        ClientInfo.GetRequestBrowser(userAgent, out var oldBrowser, out var oldBrowserVersion);
        Assert.Equal(os, oldOs);
        Assert.Equal(osVersion, oldOsVersion);
        Assert.Equal(browser, oldBrowser);
        Assert.Equal(browserVersion, oldBrowserVersion);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SAMSUNG SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/23.0 Chrome/115.0.0.0 Mobile Safari/537.36", "Samsung", "SM-S911B")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; Pixel 5 Build/RQ3A.210805.001.A1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/94.0.4606.71 Mobile Safari/537.36", "Google", "Pixel 5")]
    [InlineData("Mozilla/5.0 (Linux; U; Android 12; zh-cn; 2201123C Build/SKQ1.211006.001) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/100.0.4896.127 Mobile Safari/537.36 XiaoMi/MiuiBrowser/17.3.100105", "Xiaomi", "2201123C")]
    [InlineData("Mozilla/5.0 (Linux; Android 12; Redmi Note 11 Build/SKQ1.211103.001) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36", "Xiaomi", "Redmi Note 11")]
    [InlineData("Mozilla/5.0 (Linux; Android 12; HarmonyOS; ANA-AN00; HMSCore 6.6.0.352) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.88 HuaweiBrowser/14.0.5.302 Mobile Safari/537.36", "Huawei", "ANA-AN00")]
    [InlineData("Mozilla/5.0 (Linux; U; Android 4.0.3; ko-kr; LG-L160L Build/IML74K) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30", "LG", "LG-L160L")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; CPH2449) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36", "OPPO", "CPH2449")]
    [InlineData("Mozilla/5.0 (Linux; Android 9; KFMAWI) AppleWebKit/537.36 (KHTML, like Gecko) Silk/124.2.1 like Chrome/124.0.6367.82 Safari/537.36", "Amazon", "KFMAWI")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36", "Unknown", "")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_4_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Mobile/15E148 Safari/604.1", "Apple", "iPhone")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 330.0.3.25.90 (iPhone14,5; iOS 17_4; en_US; en; scale=3.00; 1170x2532; 597000000)", "Apple", "iPhone14,5")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1", "Apple", "iPad")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15", "Apple", "Macintosh")]
    [InlineData("Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 920)", "Nokia", "Lumia 920")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.2478.67", "Microsoft", "Xbox Series X")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Unknown", "")]
    public void ParseUserAgent_DetectsManufactureAndModel(string userAgent, string manufacture, string model)
    {
        var details = ClientInfo.ParseUserAgent(userAgent);

        Assert.Equal(manufacture, details.Manufacture);
        Assert.Equal(model, details.Model);
        Assert.Equal(model, ClientInfo.GetRequestDeviceModel(userAgent));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", "Blink", "124.0.0.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0", "Gecko", "125.0")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15", "WebKit", "605.1.15")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/124.0.6367.88 Mobile/15E148 Safari/604.1", "WebKit", "605.1.15")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko", "Trident", "7.0")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36 Edge/18.19582", "EdgeHTML", "18.19582")]
    [InlineData("Opera/9.80 (Windows NT 6.1; WOW64) Presto/2.12.388 Version/12.18", "Presto", "2.12.388")]
    public void ParseUserAgent_DetectsEngine(string userAgent, string engine, string version)
    {
        var details = ClientInfo.ParseUserAgent(userAgent);

        Assert.Equal(engine, details.EngineName);
        Assert.Equal(version, details.EngineVersion);
    }

    [Fact]
    public void ClientHints_RefineWindows11_FullVersion_AndroidModel()
    {
        var headers = new HeaderDictionary
        {
            ["Sec-CH-UA"] = "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\", \"Not-A.Brand\";v=\"99\"",
            ["Sec-CH-UA-Platform"] = "\"Windows\"",
            ["Sec-CH-UA-Platform-Version"] = "\"15.0.0\"",
            ["Sec-CH-UA-Full-Version-List"] = "\"Chromium\";v=\"124.0.6367.91\", \"Google Chrome\";v=\"124.0.6367.91\", \"Not-A.Brand\";v=\"99.0.0.0\""
        };
        var windows = ClientInfo.ParseUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", headers);
        Assert.Equal("Windows 11", windows.OsName);
        Assert.Equal("Chrome", windows.BrowserName);
        Assert.Equal("124.0.6367.91", windows.BrowserVersion);
        Assert.Equal("124.0.6367.91", windows.EngineVersion);

        headers["Sec-CH-UA-Platform-Version"] = "\"10.0.0\"";
        Assert.Equal("Windows 10", ClientInfo.ParseUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", headers).OsName);

        var android = ClientInfo.ParseUserAgent("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36", new HeaderDictionary
        {
            ["Sec-CH-UA-Mobile"] = "?1",
            ["Sec-CH-UA-Platform"] = "\"Android\"",
            ["Sec-CH-UA-Platform-Version"] = "\"14.0.0\"",
            ["Sec-CH-UA-Model"] = "\"SM-S918B\""
        });
        Assert.Equal("14", android.OsVersion);
        Assert.Equal("SM-S918B", android.Model);
        Assert.Equal("Samsung", android.Manufacture);

        var mac = ClientInfo.ParseUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", new HeaderDictionary
        {
            ["Sec-CH-UA-Platform"] = "\"macOS\"",
            ["Sec-CH-UA-Platform-Version"] = "\"14.4.1\""
        });
        Assert.Equal("14.4.1", mac.OsVersion);
    }

    [Fact]
    public void ClientHints_DetectBrave()
    {
        var details = ClientInfo.ParseUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", new HeaderDictionary
        {
            ["Sec-CH-UA"] = "\"Chromium\";v=\"124\", \"Brave\";v=\"124\", \"Not-A.Brand\";v=\"99\""
        });
        Assert.Equal("Brave", details.BrowserName);
    }

    [Fact]
    public void GetRequestDetails_UsesHeadersAndIp()
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.1.2.3");
        ctx.Request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
        ctx.Request.Headers["Sec-CH-UA-Platform"] = "\"Windows\"";
        ctx.Request.Headers["Sec-CH-UA-Platform-Version"] = "\"15.0.0\"";

        var details = ClientInfo.GetRequestDetails(ctx);
        Assert.Equal("Windows 11", details.OsName);
        Assert.Equal("10.1.2.3", details.IP);
        Assert.Equal("Desktop", details.DeviceType);

        var log = ctx.GetDeviceLog();
        Assert.Equal("Windows 11 ", log.Os);
        Assert.Equal("Chrome 124.0.0.0", log.Application);
        Assert.False(log.IsMobile);
    }

    [Fact]
    public void GetRequestDetails_WithoutUserAgent_ReturnsUnknownModel()
    {
        var details = ClientInfo.GetRequestDetails(new DefaultHttpContext());

        Assert.NotNull(details);
        Assert.Equal("Unknown", details.OsName);
        Assert.Equal("Unknown", details.BrowserName);
        Assert.Equal("0.0", details.BrowserVersion);
        Assert.Equal("Unknown", details.Manufacture);
        Assert.Equal("", details.Model);
        Assert.False(details.IsMobile);
        Assert.Null(ClientInfo.GetRequestDetails(null));
    }

    [Fact]
    public void ParseUserAgent_HostileInput_DoesNotThrow()
    {
        // Long versions are clipped; anything after the first 1024 chars is ignored (bounds the scan cost).
        var details = ClientInfo.ParseUserAgent("Mozilla/5.0 (Linux; Android " + new string('9', 5000) + "; " + new string(';', 5000));
        Assert.Equal("Android", details.OsName);
        Assert.True(details.OsVersion.Length <= 32);
        Assert.Equal("Unknown", ClientInfo.ParseUserAgent("Mozilla/5.0 (" + new string('(', 5000) + "Android 14)").OsName);

        foreach (var ua in new[] { "(", ")", "(;)", "Android", "Windows NT ", "CrOS ", "BlackBerry", "iPhone", "FBDV/", "bot/", "Mozilla/5.0 (Linux; Android 10;", "Version/", "Edge/" })
            Assert.NotNull(ClientInfo.ParseUserAgent(ua));

        Assert.Equal("Unknown", ClientInfo.ParseUserAgent(null).OsName);
    }
}
