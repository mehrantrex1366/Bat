namespace Bat.Http;

/// <summary>
/// Parses a User-Agent (and, when available, User-Agent Client Hints) into OS, browser, engine and device information.
/// Runs on the request hot path: only ordinal <c>IndexOf</c> scans and span slicing, no Regex, no exceptions.
/// Rule order matters: more specific tokens come first ("Edg/", "OPR/", "SamsungBrowser/" before "Chrome/";
/// "Windows Phone" before "Android"; "iPhone" before "Mac OS X"; "Android" / "CrOS" before "Linux").
/// </summary>
internal static class UserAgentParser
{
    internal const string Unknown = "Unknown";
    internal const string UnknownBrowserVersion = "0.0";

    internal const string Desktop = "Desktop";
    internal const string Mobile = "Mobile";
    internal const string Tablet = "Tablet";
    internal const string Tv = "TV";
    internal const string GameConsole = "Console";
    internal const string Wearable = "Wearable";
    internal const string Bot = "Bot";

    private const string Ios = "IOS";
    private const string MacOs = "Mac OS";
    private const string Android = "Android";
    private const string FireOs = "Fire OS";
    private const string HarmonyOs = "HarmonyOS";

    // Real user agents are < 500 chars; the cap bounds the cost of the rule scans for hostile headers.
    private const int MaxUserAgentLength = 1024;
    private const int MaxVersionLength = 32;
    private const int MaxNameLength = 50;

    private readonly record struct Rule(string Token, string Name, string VersionToken = null);

    // Named crawlers, link-preview fetchers and health checks (case-sensitive, as they appear in the User-Agent).
    private static readonly Rule[] _bots =
    [
        new("Googlebot", "Googlebot"),
        new("AdsBot-Google", "AdsBot-Google"),
        new("Mediapartners-Google", "Google AdSense"),
        new("Google-InspectionTool", "Google-InspectionTool"),
        new("GoogleOther", "GoogleOther"),
        new("Storebot-Google", "Storebot-Google"),
        new("APIs-Google", "APIs-Google"),
        new("FeedFetcher-Google", "FeedFetcher-Google"),
        new("GoogleHC/", "Google Health Check"),
        new("bingbot", "Bingbot"),
        new("BingPreview", "BingPreview"),
        new("msnbot", "Msnbot"),
        new("adidxbot", "Bing Ads"),
        new("Yahoo! Slurp", "Yahoo Slurp"),
        new("DuckDuckBot", "DuckDuckBot"),
        new("Baiduspider", "Baiduspider"),
        new("YandexBot", "YandexBot"),
        new("YandexMobileBot", "YandexBot"),
        new("Sogou web spider", "Sogou Spider"),
        new("Exabot", "Exabot"),
        new("SeznamBot", "SeznamBot"),
        new("PetalBot", "PetalBot"),
        new("Applebot", "Applebot"),
        new("facebookexternalhit", "Facebook Crawler"),
        new("facebookcatalog", "Facebook Crawler"),
        new("meta-externalagent", "Meta Crawler"),
        new("Twitterbot", "Twitterbot"),
        new("LinkedInBot", "LinkedInBot"),
        new("Slackbot", "Slackbot"),
        new("Slack-ImgProxy", "Slackbot"),
        new("Discordbot", "Discordbot"),
        new("TelegramBot", "TelegramBot"),
        new("WhatsApp/", "WhatsApp"),
        new("SkypeUriPreview", "Skype Preview"),
        new("Pinterestbot", "Pinterestbot"),
        new("redditbot", "Redditbot"),
        new("Embedly", "Embedly"),
        new("GPTBot", "GPTBot"),
        new("ChatGPT-User", "ChatGPT-User"),
        new("OAI-SearchBot", "OAI-SearchBot"),
        new("ClaudeBot", "ClaudeBot"),
        new("Claude-User", "Claude-User"),
        new("Claude-SearchBot", "Claude-SearchBot"),
        new("anthropic-ai", "Anthropic"),
        new("PerplexityBot", "PerplexityBot"),
        new("Perplexity-User", "Perplexity-User"),
        new("Bytespider", "Bytespider"),
        new("CCBot", "CCBot"),
        new("AhrefsBot", "AhrefsBot"),
        new("SemrushBot", "SemrushBot"),
        new("MJ12bot", "MJ12bot"),
        new("DotBot", "DotBot"),
        new("ia_archiver", "Alexa Crawler"),
        new("archive.org_bot", "Internet Archive"),
        new("UptimeRobot", "UptimeRobot"),
        new("Pingdom", "Pingdom"),
        new("StatusCake", "StatusCake"),
        new("Site24x7", "Site24x7"),
        new("Chrome-Lighthouse", "Lighthouse"),
        new("kube-probe/", "kube-probe"),
        new("ELB-HealthChecker/", "ELB-HealthChecker"),
        new("Prometheus/", "Prometheus"),
    ];

    // Generic crawler markers, checked after the named bots. Case-sensitive on purpose: "CUBOT" is a phone brand.
    private static readonly string[] _botMarkers = ["bot/", "Bot/", "bot;", "Bot;", "bot)", "Bot)", "crawler", "Crawler", "spider", "Spider", "+http"];

    // HTTP libraries and tools (no OS / browser tokens of their own).
    private static readonly Rule[] _tools =
    [
        new("PostmanRuntime/", "Postman"),
        new("Postman", "Postman"),
        new("insomnia/", "Insomnia"),
        new("HTTPie/", "HTTPie"),
        new("curl/", "curl"),
        new("Wget/", "Wget"),
        new("WindowsPowerShell/", "PowerShell"),
        new("PowerShell/", "PowerShell"),
        new("okhttp/", "OkHttp"),
        new("python-requests/", "Python Requests"),
        new("python-httpx/", "Python httpx"),
        new("Python-urllib/", "Python urllib"),
        new("aiohttp/", "aiohttp"),
        new("Go-http-client/", "Go HTTP Client"),
        new("Apache-HttpClient/", "Apache HttpClient"),
        new("Java/", "Java"),
        new("axios/", "axios"),
        new("node-fetch", "node-fetch"),
        new("undici", "undici"),
        new("Dart/", "Dart"),
        new("RestSharp/", "RestSharp"),
        new("Dalvik/", "Dalvik"),
        new("CFNetwork/", "CFNetwork"),
    ];

    // In-app and branded browsers. All of them also carry generic tokens ("Chrome/", "Safari/", "Firefox/"),
    // so they must be checked before the generic ones.
    private static readonly Rule[] _browsers =
    [
        // In-app browsers of social / messaging apps
        new("FBAV/", "Facebook"),
        new("FBAN/", "Facebook", "FBAV/"),
        new("FB_IAB", "Facebook", "FBAV/"),
        new("Instagram ", "Instagram"),
        new("BytedanceWebview", "TikTok", "app_version/"),
        new("musical_ly", "TikTok", "app_version/"),
        new("Snapchat", "Snapchat"),
        new("Pinterest/", "Pinterest"),
        new("LinkedInApp", "LinkedIn"),
        new("TwitterAndroid", "Twitter"),
        new("Twitter for iPhone", "Twitter"),
        new("MicroMessenger/", "WeChat"),
        new(" Line/", "Line"),
        new("GSA/", "Google App"),

        // Vendor, Chromium-based and Gecko-based browsers
        new("YaBrowser/", "Yandex"),
        new("YaSearchBrowser/", "Yandex"),
        new("SamsungBrowser/", "SamsungBrowser"),
        new("UCBrowser/", "UCBrowser"),
        new("UBrowser/", "UCBrowser"),
        new("UCWEB", "UCBrowser", "UCBrowser/"),
        new("MiuiBrowser/", "MIUI Browser"),
        new("HuaweiBrowser/", "Huawei Browser"),
        new("HeyTapBrowser/", "HeyTap Browser"),
        new("OppoBrowser/", "Oppo Browser"),
        new("VivoBrowser/", "Vivo Browser"),
        new("Silk/", "Silk"),
        new("DuckDuckGo/", "DuckDuckGo"),
        new("Ddg/", "DuckDuckGo"),
        new("MQQBrowser/", "QQ Browser"),
        new("QQBrowser/", "QQ Browser"),
        new("Maxthon", "Maxthon"),
        new("Whale/", "Whale"),
        new("coc_coc_browser/", "Coc Coc"),
        new("Puffin/", "Puffin"),
        new("Avast/", "Avast Secure Browser"),
        new("AVG/", "AVG Secure Browser"),
        new("Vivaldi/", "Vivaldi"),
        new("Brave/", "Brave"),
        new("Electron/", "Electron"),
        new("SeaMonkey/", "SeaMonkey"),
        new("Waterfox/", "Waterfox"),
        new("PaleMoon/", "Pale Moon"),
        new("Iceweasel/", "Iceweasel"),
        new("Konqueror/", "Konqueror"),
        new("Epiphany/", "GNOME Web"),
        new("Midori/", "Midori"),
        new("Falkon/", "Falkon"),
        new("QtWebEngine/", "QtWebEngine"),
        new("PhantomJS/", "PhantomJS"),
        new("HeadlessChrome/", "Chrome Headless"),
        new("Focus/", "Firefox Focus"),
        new("Klar/", "Firefox Klar"),

        // Opera family ("OPR/" user agents also contain "Chrome/")
        new("Opera Mini/", "Opera Mini"),
        new("Opera Mobi", "Opera Mobile", "Version/"),
        new("OPR/", "Opera"),
        new("OPiOS/", "Opera"),
        new("OPT/", "Opera Touch"),
        new("Opera/", "Opera", "Version/"),
        new("Opera ", "Opera"),

        // Microsoft ("Edg/" is Chromium Edge, "Edge/" is legacy EdgeHTML Edge)
        new("EdgA/", "Edge"),
        new("EdgiOS/", "Edge"),
        new("Edg/", "Edge"),
        new("Edge/", "Edge"),
        new("IEMobile", "IE Mobile"),
        new("MSIE ", "Internet Explorer"),
        new("Trident/", "Internet Explorer", "rv:"),

        // Generic, but must win over "Chrome/" / "Safari/"
        new("CriOS/", "Chrome"),
        new("Chromium/", "Chromium"),
    ];

    // Desktop operating systems other than Windows / macOS / Chrome OS, checked before the generic "Linux".
    private static readonly Rule[] _otherOs =
    [
        new("Kubuntu", "Kubuntu"),
        new("Xubuntu", "Xubuntu"),
        new("Lubuntu", "Lubuntu"),
        new("Ubuntu", "Ubuntu"),
        new("Linux Mint", "Linux Mint"),
        new("Debian", "Debian"),
        new("Fedora", "Fedora"),
        new("Red Hat", "Red Hat"),
        new("CentOS", "CentOS"),
        new("openSUSE", "openSUSE"),
        new("SUSE", "SUSE"),
        new("Arch Linux", "Arch Linux"),
        new("Manjaro", "Manjaro"),
        new("Gentoo", "Gentoo"),
        new("Slackware", "Slackware"),
        new("Mageia", "Mageia"),
        new("Raspbian", "Raspbian"),
        new("elementary OS", "elementary OS"),
        new("FreeBSD", "FreeBSD"),
        new("OpenBSD", "OpenBSD"),
        new("NetBSD", "NetBSD"),
        new("DragonFly", "DragonFly BSD"),
        new("SunOS", "Solaris"),
        new("Solaris", "Solaris"),
        new("Haiku", "Haiku"),
        new("BeOS", "BeOS"),
        new("Fuchsia", "Fuchsia"),
    ];

    // Model prefix -> manufacturer (case-insensitive, first match wins).
    private static readonly (string Prefix, string Brand)[] _brands =
    [
        ("SAMSUNG", "Samsung"), ("SM-", "Samsung"), ("GT-", "Samsung"), ("SCH-", "Samsung"), ("SGH-", "Samsung"),
        ("SHV-", "Samsung"), ("SPH-", "Samsung"), ("Galaxy", "Samsung"),
        ("Pixel", "Google"), ("Nexus", "Google"),
        ("Redmi", "Xiaomi"), ("POCO", "Xiaomi"), ("Xiaomi", "Xiaomi"), ("MI ", "Xiaomi"), ("HM ", "Xiaomi"),
        ("HUAWEI", "Huawei"), ("HONOR", "Honor"),
        ("OnePlus", "OnePlus"),
        ("CPH", "OPPO"), ("OPPO", "OPPO"),
        ("RMX", "Realme"), ("realme", "Realme"),
        ("vivo", "vivo"),
        ("motorola", "Motorola"), ("moto", "Motorola"), ("XT", "Motorola"),
        ("LG", "LG"), ("LM-", "LG"),
        ("Nokia", "Nokia"), ("TA-", "Nokia"),
        ("Xperia", "Sony"), ("SO-", "Sony"), ("Sony", "Sony"), ("BRAVIA", "Sony"),
        ("ASUS", "Asus"), ("ZenFone", "Asus"),
        ("Lenovo", "Lenovo"), ("TB-", "Lenovo"),
        ("TECNO", "Tecno"), ("Infinix", "Infinix"), ("itel", "itel"),
        ("HTC", "HTC"), ("ZTE", "ZTE"), ("Meizu", "Meizu"), ("Alcatel", "Alcatel"), ("TCL", "TCL"),
        ("Nothing", "Nothing"), ("Fairphone", "Fairphone"), ("Hisense", "Hisense"),
        ("Blackview", "Blackview"), ("Ulefone", "Ulefone"), ("OUKITEL", "Oukitel"), ("DOOGEE", "Doogee"), ("CUBOT", "Cubot"),
        ("Lumia", "Microsoft"), ("KF", "Amazon"), ("AFT", "Amazon"),
    ];

    // Smart TV / set-top box markers.
    private static readonly string[] _tvMarkers =
    [
        "SmartTV", "SMART-TV", "Smart-TV", "SmartTv", "SMARTTV", "GoogleTV", "Google TV", "Android TV", "AndroidTV",
        "BRAVIA", "HbbTV", "NetCast", "Web0S", "webOS.TV", "TV Safari", "DTV", "AppleTV", "Apple TV", "CrKey", "Roku",
    ];

    internal static RequestDetails Parse(string userAgent, IHeaderDictionary headers)
    {
        var ua = Normalize(userAgent);
        ParseOs(ua, out var os, out var osVersion);
        ParseBrowser(ua, os, out var browser, out var browserVersion, out var isBot);
        ParseEngine(ua, os, out var engine, out var engineVersion);
        ParseDevice(ua, os, isBot, out var deviceType, out var manufacture, out var model);

        var result = new RequestDetails
        {
            OsName = os,
            OsVersion = osVersion,
            BrowserName = browser,
            BrowserVersion = browserVersion,
            EngineName = engine,
            EngineVersion = engineVersion,
            DeviceType = deviceType,
            Manufacture = manufacture,
            Model = model,
            IsMobile = IsHandheld(deviceType),
            IsTablet = deviceType == Tablet,
            IsBot = isBot
        };

        if (headers != null && !isBot) ApplyClientHints(headers, result);
        return result;
    }

    internal static string Normalize(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return string.Empty;
        return userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
    }

    #region OS

    internal static void ParseOs(string ua, out string os, out string version)
    {
        version = string.Empty;
        if (ua.Length == 0) { os = Unknown; return; }

        // Windows Phone 8.1+/10 user agents also contain "Android" and "iPhone OS".
        if (ua.Contains("Windows Phone")) { os = "Windows Phone"; version = FirstVersion(ua, "Windows Phone OS", "Windows Phone"); }
        else if (ua.Contains("Windows Mobile") || ua.Contains("Windows CE") || ua.Contains("IEMobile")) { os = "Windows Mobile"; version = VersionAfter(ua, "Windows Mobile"); }
        // Consoles before Windows / Linux: Xbox sends "Windows NT 10.0".
        else if (ua.Contains("Xbox")) os = "Xbox";
        else if (ua.Contains("PlayStation") || ua.Contains("PLAYSTATION")) os = "PlayStation";
        else if (ua.Contains("Nintendo")) os = "Nintendo";
        // Android-derived / Linux-based systems before "Android" and "Linux".
        else if (ua.Contains("OpenHarmony")) { os = HarmonyOs; version = VersionAfter(ua, "OpenHarmony"); }
        else if (ua.Contains("HarmonyOS")) { os = HarmonyOs; version = VersionAfter(ua, "HarmonyOS"); }
        else if (ua.Contains("KAIOS", StringComparison.OrdinalIgnoreCase)) { os = "KaiOS"; version = VersionAfter(ua, "KAIOS", StringComparison.OrdinalIgnoreCase); }
        else if (ua.Contains("Tizen")) { os = "Tizen"; version = VersionAfter(ua, "Tizen"); }
        else if (ua.Contains("Web0S") || ua.Contains("webOS") || ua.Contains("hpwOS")) { os = "webOS"; version = FirstVersion(ua, "webOS", "hpwOS"); }
        else if (ua.Contains("CrKey/")) { os = "Chromecast"; version = VersionAfter(ua, "CrKey/"); }
        else if (ua.Contains("Roku")) { os = "Roku OS"; version = VersionAfter(ua, "Roku/DVP"); }
        else if (IsAmazonDevice(ua)) os = FireOs;
        else if (ua.Contains("Android")) { os = Android; version = VersionAfter(ua, "Android"); }
        else if (ua.Contains("Adr ")) { os = Android; version = VersionAfter(ua, "Adr "); }
        else if (ua.Contains("BlackBerry") || ua.Contains("BB10") || ua.Contains("RIM Tablet OS") || ua.Contains("PlayBook")) { os = "Black Berry"; version = BlackBerryVersion(ua); }
        else if (ua.Contains("Symbian") || ua.Contains("SymbOS") || ua.Contains("Series60")) { os = "Symbian"; version = FirstVersion(ua, "SymbianOS/", "Symbian/", "Series60/"); }
        else if (ua.Contains("Sailfish")) os = "Sailfish OS";
        else if ((ua.Contains("(Mobile;") || ua.Contains("(Tablet;")) && ua.Contains("Firefox/")) os = "Firefox OS";
        // Apple: iPhone/iPad user agents also contain "Mac OS X".
        else if (ua.Contains("Apple Watch") || ua.Contains("watchOS")) { os = "watchOS"; version = FirstVersion(ua, "watchOS", "Watch OS"); }
        else if (ua.Contains("AppleTV") || ua.Contains("Apple TV") || ua.Contains("tvOS")) { os = "tvOS"; version = FirstVersion(ua, "tvOS", "CPU OS"); }
        else if (ua.Contains("iPhone") || ua.Contains("iPad") || ua.Contains("iPod")) { os = Ios; version = IosVersion(ua); }
        else if (ua.Contains("CrOS")) { os = "Chrome OS"; version = ChromeOsVersion(ua); }
        // macOS user agents are frozen at "10_15_7" since Big Sur; Sec-CH-UA-Platform-Version has the real version.
        else if (ua.Contains("Macintosh") || ua.Contains("Mac OS") || ua.Contains("Mac_PowerPC")) { os = MacOs; version = VersionAfter(ua, "Mac OS X"); }
        else if (ua.Contains("Windows") || ua.Contains("Win32") || ua.Contains("Win64") || ua.Contains("Win98") || ua.Contains("Win95") || ua.Contains("Win 9x")) os = WindowsName(ua, out version);
        else if (TryMatch(ua, _otherOs, out var other, out var otherVersion)) { os = other; version = otherVersion; }
        else if (ua.Contains("Linux") || ua.Contains("X11")) os = "Linux";
        else if (ua.Contains("Postman")) { os = "Postman"; version = VersionAfter(ua, "PostmanRuntime/"); }
        else os = Unknown;
    }

    private static string WindowsName(string ua, out string version)
    {
        version = string.Empty;

        // Searching "Windows NT " (with the space) skips "Windows NT;" in PowerShell's user agent.
        var nt = VersionAfter(ua, "Windows NT ");
        if (nt.Length > 0)
        {
            // Windows 11 also sends "Windows NT 10.0"; only Sec-CH-UA-Platform-Version tells them apart (see ApplyClientHints).
            if (nt.StartsWith("10", StringComparison.Ordinal)) return "Windows 10";
            if (nt.StartsWith("11", StringComparison.Ordinal)) return "Windows 11";
            if (nt.StartsWith("6.3", StringComparison.Ordinal)) return "Windows 8.1";
            if (nt.StartsWith("6.2", StringComparison.Ordinal)) return "Windows 8";
            if (nt.StartsWith("6.1", StringComparison.Ordinal)) return "Windows 7";
            if (nt.StartsWith("6.0", StringComparison.Ordinal)) return "Windows Vista";
            if (nt.StartsWith("5.01", StringComparison.Ordinal) || nt.StartsWith("5.0", StringComparison.Ordinal)) return "Windows 2000";
            if (nt.StartsWith("5.1", StringComparison.Ordinal) || nt.StartsWith("5.2", StringComparison.Ordinal)) return "Windows XP";

            version = nt;
            return "Windows NT";
        }

        if (ua.Contains("Windows 98") || ua.Contains("Win98")) return "Windows 98";
        if (ua.Contains("Windows ME") || ua.Contains("Win 9x 4.90")) return "Windows ME";
        if (ua.Contains("Windows 95") || ua.Contains("Win95")) return "Windows 95";
        return "Windows";
    }

    private static string IosVersion(string ua)
    {
        var version = FirstVersion(ua, "iPhone OS", "CPU OS", "iOS");

        // Since iOS 26, Safari freezes the OS in the User-Agent at "18_6"; the real version is in "Version/26.x".
        var safari = VersionAfter(ua, "Version/");
        var safariMajor = Major(safari);
        return safariMajor >= 26 && safariMajor > Major(version) ? safari : version;
    }

    // "X11; CrOS x86_64 15359.58.0" -> skip the CPU architecture, read the platform version.
    private static string ChromeOsVersion(string ua)
    {
        var index = ua.IndexOf("CrOS ", StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        var rest = ua.AsSpan(index + 5);
        var space = rest.IndexOf(' ');
        return space < 0 ? string.Empty : ReadVersion(rest[(space + 1)..]);
    }

    private static string BlackBerryVersion(string ua)
    {
        var version = FirstVersion(ua, "RIM Tablet OS", "Version/");
        if (version.Length > 0) return version;

        // Legacy "BlackBerry9700/5.0.0.351": the version follows the model number.
        var index = ua.IndexOf("BlackBerry", StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        var rest = ua.AsSpan(index + 10);
        var i = 0;
        while (i < rest.Length && char.IsAsciiDigit(rest[i])) i++;
        return i > 0 && i < rest.Length && rest[i] == '/' ? ReadVersion(rest[(i + 1)..]) : string.Empty;
    }

    // Kindle / Fire tablets ("KFxxWI") and Fire TV ("AFTxx") run Fire OS (an Android fork).
    private static bool IsAmazonDevice(string ua) =>
        ua.Contains("Kindle") || ua.Contains("Silk/") || HasUpperCode(ua, "; KF") || HasUpperCode(ua, "; AFT");

    private static bool HasUpperCode(string ua, string prefix)
    {
        var index = ua.IndexOf(prefix, StringComparison.Ordinal);
        if (index < 0) return false;

        var start = index + prefix.Length;
        return start + 1 < ua.Length && char.IsAsciiLetterUpper(ua[start]) && char.IsAsciiLetterUpper(ua[start + 1]);
    }

    private static bool IsAndroidFamily(string os) => os is Android or FireOs or HarmonyOs;

    #endregion

    #region Browser

    internal static void ParseBrowser(string ua, string os, out string name, out string version, out bool isBot)
    {
        isBot = false;
        if (ua.Length == 0) { name = Unknown; version = UnknownBrowserVersion; return; }

        if (TryMatch(ua, _bots, out name, out version) || TryMatchBotMarker(ua, out name, out version)) { isBot = true; return; }
        if (TryMatch(ua, _tools, out name, out version)) return;
        if (TryMatch(ua, _browsers, out name, out version)) return;

        var android = IsAndroidFamily(os);
        if (ua.Contains("Chrome/"))
        {
            // Android System WebView: "; wv)" since Android 5, "Version/4.0 Chrome/..." on Android 4.4.
            name = android && (ua.Contains("; wv)") || ua.Contains("Version/")) ? "Chrome WebView" : "Chrome";
            version = VersionAfter(ua, "Chrome/");
        }
        else if (ua.Contains("FxiOS/")) { name = "Firefox"; version = VersionAfter(ua, "FxiOS/"); }
        else if (ua.Contains("Firefox/")) { name = "Firefox"; version = VersionAfter(ua, "Firefox/"); }
        // Fixed: Safari's version is in "Version/x.y"; "Safari/604.1" is the WebKit build number.
        else if (ua.Contains("Safari/")) { name = android ? "Android Browser" : "Safari"; version = VersionAfter(ua, "Version/"); }
        // WKWebView inside iOS apps: WebKit without a "Safari/" token.
        else if (os == Ios && ua.Contains("AppleWebKit/")) { name = "iOS WebView"; version = string.Empty; }
        else { name = Unknown; version = UnknownBrowserVersion; }
    }

    private static bool TryMatchBotMarker(string ua, out string name, out string version)
    {
        // The earliest marker wins: in "SomeCrawler/1.0; +https://example.com/crawler" the product token comes before the URL.
        var index = -1;
        var marker = string.Empty;
        foreach (var candidate in _botMarkers)
        {
            var found = ua.IndexOf(candidate, StringComparison.Ordinal);
            if (found >= 0 && (index < 0 || found < index)) { index = found; marker = candidate; }
        }

        name = null;
        version = null;
        if (index < 0) return false;

        name = Bot;
        version = string.Empty;
        if (marker == "+http") return true;

        // The product token that contains the marker: "MJ12bot/v1.4.8" -> name "MJ12bot", version "1.4.8".
        var start = index;
        while (start > 0 && !IsTokenDelimiter(ua[start - 1])) start--;
        var end = index;
        while (end < ua.Length && ua[end] != '/' && !IsTokenDelimiter(ua[end])) end++;

        var token = ua.AsSpan(start, end - start);
        if (token.IsEmpty || token.Contains(':')) return true;

        name = Clip(token);
        if (end < ua.Length && ua[end] == '/') version = ReadVersion(ua.AsSpan(end + 1));
        return true;
    }

    private static bool IsTokenDelimiter(char c) => c is ' ' or ';' or '(' or ')' or ',' or '+';

    #endregion

    #region Engine

    internal static void ParseEngine(string ua, string os, out string engine, out string version)
    {
        version = string.Empty;
        if (ua.Contains("Trident/")) { engine = "Trident"; version = VersionAfter(ua, "Trident/"); }
        else if (ua.Contains("MSIE ")) engine = "Trident";
        else if (ua.Contains("Edge/")) { engine = "EdgeHTML"; version = VersionAfter(ua, "Edge/"); }
        else if (ua.Contains("Presto/")) { engine = "Presto"; version = VersionAfter(ua, "Presto/"); }
        // Every browser on iOS (Chrome, Firefox, Edge, ...) must use WebKit.
        else if (os is Ios or "watchOS" or "tvOS" && ua.Contains("AppleWebKit/")) { engine = "WebKit"; version = VersionAfter(ua, "AppleWebKit/"); }
        else if (ua.Contains("Chrome/") || ua.Contains("Chromium/"))
        {
            version = FirstVersion(ua, "Chrome/", "Chromium/");
            // Blink was forked from WebKit in Chrome 28.
            engine = Major(version) is >= 28 or < 0 ? "Blink" : "WebKit";
        }
        else if (ua.Contains("Goanna/")) { engine = "Goanna"; version = VersionAfter(ua, "Goanna/"); }
        else if (ua.Contains("Gecko/")) { engine = "Gecko"; version = VersionAfter(ua, "rv:"); }
        else if (ua.Contains("AppleWebKit/")) { engine = "WebKit"; version = VersionAfter(ua, "AppleWebKit/"); }
        else if (ua.Contains("KHTML")) { engine = "KHTML"; version = VersionAfter(ua, "KHTML/"); }
        else engine = Unknown;
    }

    #endregion

    #region Device

    internal static void ParseDevice(string ua, string os, bool isBot, out string deviceType, out string manufacture, out string model)
    {
        manufacture = Unknown;
        model = string.Empty;
        deviceType = DeviceTypeOf(ua, os, isBot);
        if (ua.Length == 0) return;

        switch (os)
        {
            case Ios:
                manufacture = "Apple";
                model = ua.Contains("iPad") ? AppleModel(ua, "iPad")
                    : ua.Contains("iPod") ? AppleModel(ua, "iPod", "iPod touch")
                    : AppleModel(ua, "iPhone");
                break;
            case MacOs: manufacture = "Apple"; model = "Macintosh"; break;
            case "watchOS": manufacture = "Apple"; model = "Apple Watch"; break;
            case "tvOS": manufacture = "Apple"; model = "Apple TV"; break;
            case "Xbox": manufacture = "Microsoft"; model = FirstToken(ua, "Xbox Series X", "Xbox Series S", "Xbox One") ?? "Xbox"; break;
            case "PlayStation": manufacture = "Sony"; model = FirstToken(ua, "PlayStation 5", "PlayStation 4", "PlayStation Vita", "PLAYSTATION 3") ?? "PlayStation"; break;
            case "Nintendo": manufacture = "Nintendo"; model = FirstToken(ua, "Nintendo Switch", "Nintendo WiiU", "Nintendo 3DS", "Nintendo Wii") ?? "Nintendo"; break;
            case "Chromecast": manufacture = "Google"; model = "Chromecast"; break;
            case "Roku OS": manufacture = "Roku"; model = "Roku"; break;
            case "Chrome OS": model = "Chromebook"; break;
            case "Black Berry": manufacture = "BlackBerry"; model = BlackBerryModel(ua); break;
            case "Windows Phone": WindowsPhoneDevice(ua, ref manufacture, ref model); break;
            // LG TVs: "(Web0S; Linux/SmartTV)" has no model.
            case "webOS": if (ua.Contains("Web0S") || ua.Contains("LG")) manufacture = "LG"; break;
            case Android or FireOs or HarmonyOs or "Tizen" or "KaiOS":
                model = ModelAfter(ua, os switch { "Tizen" => "Tizen", "KaiOS" => null, _ => "Android" });
                SetBrand(ref manufacture, ref model);
                if (manufacture == Unknown)
                    manufacture = os switch { FireOs => "Amazon", HarmonyOs => "Huawei", "Tizen" => "Samsung", _ => Unknown };
                break;
        }
    }

    private static string DeviceTypeOf(string ua, string os, bool isBot)
    {
        if (isBot) return Bot;
        if (ua.Length == 0) return Unknown;

        switch (os)
        {
            case "Xbox" or "PlayStation" or "Nintendo": return GameConsole;
            case "Chromecast" or "Roku OS" or "tvOS": return Tv;
            case "watchOS": return Wearable;
        }

        if (ContainsAny(ua, _tvMarkers) || HasUpperCode(ua, "; AFT") || (os == "Tizen" && ua.Contains("TV"))) return Tv;
        if (ua.Contains("Wear OS") || ua.Contains("Watch;")) return Wearable;
        if (os == Ios) return ua.Contains("iPad") ? Tablet : Mobile;
        if (ua.Contains("Tablet") || ua.Contains("PlayBook") || ua.Contains("Kindle")) return Tablet;

        if (IsAndroidFamily(os))
        {
            // Android phones send "Mobile"; Android tablets don't.
            if (ua.Contains("Mobile") || ua.Contains("Mobi") || ua.Contains("Opera Mini") || ua.Contains("Dalvik/")) return Mobile;
            return Tablet;
        }

        if (ua.Contains("Mobile") || ua.Contains("Mobi") || ua.Contains("Opera Mini")
            || os is "Windows Phone" or "Windows Mobile" or "Black Berry" or "Symbian" or "KaiOS" or "Sailfish OS" or "Firefox OS" or "Tizen" or "webOS")
            return Mobile;

        return os is Unknown or "Postman" ? Unknown : Desktop;
    }

    internal static bool IsHandheld(string deviceType) => deviceType is Mobile or Tablet or Wearable;

    // "iPhone14,5" (sent by some in-app browsers) or just the family name.
    private static string AppleModel(string ua, string family, string fallback = null)
    {
        var index = 0;
        while ((index = ua.IndexOf(family, index, StringComparison.Ordinal)) >= 0)
        {
            var start = index;
            index += family.Length;
            if (index >= ua.Length || !char.IsAsciiDigit(ua[index])) continue;

            var end = index;
            while (end < ua.Length && (char.IsAsciiDigit(ua[end]) || ua[end] == ',')) end++;
            return ua[start..end];
        }

        return fallback ?? family;
    }

    private static string BlackBerryModel(string ua)
    {
        if (ua.Contains("PlayBook")) return "PlayBook";

        var index = ua.IndexOf("BlackBerry", StringComparison.Ordinal);
        if (index >= 0)
        {
            var rest = ua.AsSpan(index + 10).TrimStart(' ');
            var i = 0;
            while (i < rest.Length && char.IsAsciiDigit(rest[i])) i++;
            if (i > 0) return string.Concat("BlackBerry ", rest[..i]);
        }

        return ua.Contains("BB10") ? "BB10" : string.Empty;
    }

    // "(... IEMobile/11.0; NOKIA; Lumia 635)": the last two segments are manufacturer and model.
    private static void WindowsPhoneDevice(string ua, ref string manufacture, ref string model)
    {
        if (!TryGetGroup(ua, ua.IndexOf("Windows Phone", StringComparison.Ordinal), out var group)) return;

        var last = group.LastIndexOf(';');
        if (last < 0) return;

        model = Clip(group[(last + 1)..].Trim());
        var previous = group[..last];
        var brand = Clip(previous[(previous.LastIndexOf(';') + 1)..].Trim());
        manufacture = BrandFromModel(brand) ?? (brand.Length > 0 ? brand : Unknown);
    }

    // The device model in "(Linux; Android 13; SM-S911B Build/TP1A)" is the first meaningful segment after the anchor.
    // anchor == null: the first "(...)" group, every segment is a candidate.
    private static string ModelAfter(string ua, string anchor)
    {
        // Facebook's in-app browser sends the real model even when Chrome reduces the User-Agent.
        var facebook = TokenValue(ua, "FBDV/");
        if (facebook.Length > 0) return facebook;

        var anchorIndex = anchor == null ? -1 : ua.IndexOf(anchor, StringComparison.Ordinal);
        if (!TryGetGroup(ua, anchorIndex >= 0 ? anchorIndex : ua.IndexOf('('), out var group)) return string.Empty;

        var afterAnchor = anchorIndex < 0;
        foreach (var range in group.Split(';'))
        {
            var segment = group[range].Trim();
            if (!afterAnchor)
            {
                afterAnchor = segment.StartsWith(anchor, StringComparison.Ordinal);
                continue;
            }

            var build = segment.IndexOf("Build", StringComparison.Ordinal);
            if (build >= 0) segment = segment[..build].TrimEnd();
            if (!IsModelNoise(segment)) return Clip(segment);
        }

        return string.Empty;
    }

    private static bool IsModelNoise(ReadOnlySpan<char> segment)
    {
        // Single letters: "U" (encryption), "K" (Chrome's reduced User-Agent hides the model), "I", "N".
        if (segment.Length <= 1) return true;
        if (segment is "wv" or "Mobile" or "Tablet" or "Linux" or "X11" or "Touch" or "ARM" or "Phone" or "compatible" or "SMART-TV" or "SmartTV") return true;
        if (segment.StartsWith("rv:") || segment.StartsWith("Linux ") || segment.StartsWith("Android") || segment.StartsWith("HarmonyOS")
            || segment.StartsWith("OpenHarmony") || segment.StartsWith("HMSCore") || segment.StartsWith("Tizen") || segment.StartsWith("Opera")
            || segment.StartsWith("Adr ") || segment.StartsWith("CPU "))
            return true;

        // Language tags: "en", "en-us", "zh_CN".
        if (segment.Length == 2 && char.IsAsciiLetter(segment[0]) && char.IsAsciiLetter(segment[1])) return true;
        return segment.Length == 5 && segment[2] is '-' or '_' && char.IsAsciiLetter(segment[0]) && char.IsAsciiLetter(segment[1])
            && char.IsAsciiLetter(segment[3]) && char.IsAsciiLetter(segment[4]);
    }

    private static void SetBrand(ref string manufacture, ref string model)
    {
        if (model.Length == 0) return;

        // Samsung Internet sends "SAMSUNG SM-S911B"; keep only the model code.
        if (model.StartsWith("SAMSUNG ", StringComparison.OrdinalIgnoreCase)) model = model[8..].Trim();
        manufacture = BrandFromModel(model) ?? manufacture;
    }

    internal static string BrandFromModel(string model)
    {
        if (string.IsNullOrEmpty(model)) return null;

        foreach (var (prefix, brand) in _brands)
            if (model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return brand;

        if (IsHuaweiCode(model)) return "Huawei";
        if (IsXiaomiCode(model)) return "Xiaomi";
        if (IsVivoCode(model)) return "vivo";
        return null;
    }

    // Huawei / Honor model codes: "ELE-L29", "ANA-AN00", "MAR-LX1A", "NOH-NX9".
    private static bool IsHuaweiCode(string model)
    {
        if (model.Length is < 6 or > 9 || model[3] != '-') return false;
        if (!char.IsAsciiLetterUpper(model[0]) || !char.IsAsciiLetterUpper(model[1]) || !char.IsAsciiLetterUpper(model[2])) return false;

        var hasDigit = false;
        for (var i = 4; i < model.Length; i++)
        {
            if (char.IsAsciiDigit(model[i])) hasDigit = true;
            else if (!char.IsAsciiLetterUpper(model[i])) return false;
        }

        return hasDigit;
    }

    // Xiaomi / Redmi / POCO numeric model codes: "2201117TG", "23078PND5G", "M2101K6G".
    private static bool IsXiaomiCode(string model)
    {
        if (model.Length is < 8 or > 11) return false;
        foreach (var c in model)
            if (!char.IsAsciiDigit(c) && !char.IsAsciiLetterUpper(c)) return false;

        var digits = model[0] == 'M' ? model.AsSpan(1, 4) : model.AsSpan(0, 4);
        return digits[0] == '2' && !digits.ContainsAnyExcept("0123456789");
    }

    // vivo model codes: "V2111", "V2025A".
    private static bool IsVivoCode(string model) =>
        model.Length is 5 or 6 && model[0] == 'V' && !model.AsSpan(1, 4).ContainsAnyExcept("0123456789");

    #endregion

    #region Client hints

    // User-Agent Client Hints (Chromium browsers, HTTPS only). Low-entropy hints (Sec-CH-UA, -Mobile, -Platform) are sent
    // by default; the others only after the server asks for them with "Accept-CH" (see ClientInfo.AcceptClientHints).
    private static void ApplyClientHints(IHeaderDictionary headers, RequestDetails result)
    {
        var platform = HintValue(headers, "Sec-CH-UA-Platform");
        var platformVersion = TrimZeroSegments(HintValue(headers, "Sec-CH-UA-Platform-Version"));
        if (platform.Length > 0)
        {
            if (result.OsName == Unknown)
            {
                result.OsName = platform switch
                {
                    "macOS" => MacOs,
                    "iOS" => Ios,
                    "Chrome OS" or "Chromium OS" => "Chrome OS",
                    _ => platform
                };
            }

            if (platformVersion.Length > 0)
            {
                if (platform == "Windows" && (result.OsName == "Windows 10" || result.OsName == "Windows 11"))
                {
                    // 1-10 = Windows 10, 13+ = Windows 11, 0 = Windows 7/8/8.1 (keep what the User-Agent said).
                    var major = Major(platformVersion);
                    if (major >= 13) result.OsName = "Windows 11";
                    else if (major >= 1) result.OsName = "Windows 10";
                }
                else if ((platform == "macOS" && result.OsName == MacOs)
                    || (platform == "Android" && IsAndroidFamily(result.OsName))
                    || (platform is "Chrome OS" or "Chromium OS" && result.OsName == "Chrome OS"))
                {
                    result.OsVersion = platformVersion;
                }
            }
        }

        var model = HintValue(headers, "Sec-CH-UA-Model");
        if (model.Length > 0)
        {
            var manufacture = result.Manufacture;
            model = Clip(model);
            SetBrand(ref manufacture, ref model);
            result.Model = model;
            result.Manufacture = manufacture;
        }

        if (HintValue(headers, "Sec-CH-UA-Mobile") == "?1" && result.DeviceType is Desktop or Unknown)
        {
            result.DeviceType = Mobile;
            result.IsMobile = true;
        }

        if (result.EngineName != "Blink") return;

        // Brave sends the same User-Agent as Chrome; only its brand list tells them apart.
        var brands = headers["Sec-CH-UA"].ToString();
        var fullVersions = headers["Sec-CH-UA-Full-Version-List"].ToString();
        if (result.BrowserName == "Chrome" && (brands.Contains("\"Brave\"") || fullVersions.Contains("\"Brave\""))) result.BrowserName = "Brave";

        // Chrome reduces the User-Agent version to "124.0.0.0"; the full version list has "124.0.6367.91".
        if (fullVersions.Length == 0) return;

        var brand = result.BrowserName switch
        {
            "Chrome" => "Google Chrome",
            "Edge" => "Microsoft Edge",
            "Opera" => "Opera",
            "Brave" => "Brave",
            "Yandex" => "YaBrowser",
            "SamsungBrowser" => "Samsung Internet",
            "Chrome WebView" => "Android WebView",
            "Chromium" => "Chromium",
            _ => null
        };

        var browserVersion = BrandVersion(fullVersions, brand);
        if (browserVersion.Length > 0) result.BrowserVersion = browserVersion;

        var engineVersion = BrandVersion(fullVersions, "Chromium");
        if (engineVersion.Length > 0) result.EngineVersion = engineVersion;
    }

    private static string HintValue(IHeaderDictionary headers, string name)
    {
        var value = headers[name].ToString().AsSpan().Trim().Trim('"');
        return value.IsEmpty ? string.Empty : Clip(value);
    }

    // Sec-CH-UA-Full-Version-List: "Chromium";v="124.0.6367.91", "Google Chrome";v="124.0.6367.91", "Not-A.Brand";v="99.0.0.0"
    private static string BrandVersion(string list, string brand)
    {
        if (brand == null) return string.Empty;

        var key = string.Concat("\"", brand, "\";v=\"");
        var index = list.IndexOf(key, StringComparison.Ordinal);
        return index < 0 ? string.Empty : ReadVersion(list.AsSpan(index + key.Length));
    }

    // "15.0.0" -> "15", "14.4.1" -> "14.4.1".
    private static string TrimZeroSegments(string version)
    {
        var span = version.AsSpan();
        while (span.EndsWith(".0")) span = span[..^2];
        return span.Length == version.Length ? version : span.ToString();
    }

    #endregion

    #region Helpers

    private static bool TryMatch(string ua, Rule[] rules, out string name, out string version)
    {
        foreach (var rule in rules)
        {
            var index = ua.IndexOf(rule.Token, StringComparison.Ordinal);
            if (index < 0) continue;

            name = rule.Name;
            version = rule.VersionToken == null ? string.Empty : VersionAfter(ua, rule.VersionToken);
            if (version.Length == 0) version = ReadVersion(ua.AsSpan(index + rule.Token.Length));
            return true;
        }

        name = null;
        version = null;
        return false;
    }

    internal static string VersionAfter(string ua, string token, StringComparison comparison = StringComparison.Ordinal)
    {
        var index = ua.IndexOf(token, comparison);
        return index < 0 ? string.Empty : ReadVersion(ua.AsSpan(index + token.Length));
    }

    private static string FirstVersion(string ua, params ReadOnlySpan<string> tokens)
    {
        foreach (var token in tokens)
        {
            var version = VersionAfter(ua, token);
            if (version.Length > 0) return version;
        }

        return string.Empty;
    }

    // Reads "12.3_4" style versions ('_' -> '.') after optional separators: "Chrome/120", "Android 14", "rv:120", "MSIE 10.0", "v1.4".
    internal static string ReadVersion(ReadOnlySpan<char> text)
    {
        var i = 0;
        while (i < text.Length && text[i] is ' ' or '/' or ':' or '-') i++;
        if (i + 1 < text.Length && text[i] is 'v' or 'V' && char.IsAsciiDigit(text[i + 1])) i++;

        var start = i;
        while (i < text.Length && i - start < MaxVersionLength && (char.IsAsciiDigit(text[i]) || text[i] == '.' || text[i] == '_')) i++;

        var version = text[start..i].TrimEnd("._");
        if (version.IsEmpty || !char.IsAsciiDigit(version[0])) return string.Empty;

        return version.Contains('_') ? version.ToString().Replace('_', '.') : version.ToString();
    }

    // "FBDV/SM-G991B;FBMD/..." -> "SM-G991B"
    private static string TokenValue(string ua, string token)
    {
        var index = ua.IndexOf(token, StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        var rest = ua.AsSpan(index + token.Length);
        var end = rest.IndexOfAny(';', ']', ')');
        return Clip((end < 0 ? rest : rest[..end]).Trim());
    }

    // The "(...)" group that contains position `index`.
    private static bool TryGetGroup(string ua, int index, out ReadOnlySpan<char> group)
    {
        group = default;
        if (index < 0) return false;

        var open = ua[index] == '(' ? index : ua.LastIndexOf('(', index);
        if (open < 0) return false;

        var close = ua.IndexOf(')', index);
        if (close < 0) close = ua.Length;

        group = ua.AsSpan(open + 1, close - open - 1);
        return true;
    }

    private static string FirstToken(string ua, params ReadOnlySpan<string> tokens)
    {
        foreach (var token in tokens)
            if (ua.Contains(token)) return token;

        return null;
    }

    private static bool ContainsAny(string ua, string[] tokens)
    {
        foreach (var token in tokens)
            if (ua.Contains(token)) return true;

        return false;
    }

    private static int Major(string version)
    {
        var value = 0;
        var i = 0;
        for (; i < version.Length && i < 9 && char.IsAsciiDigit(version[i]); i++) value = value * 10 + (version[i] - '0');
        return i == 0 ? -1 : value;
    }

    private static string Clip(ReadOnlySpan<char> value) =>
        (value.Length > MaxNameLength ? value[..MaxNameLength] : value).ToString();

    private static string Clip(string value) => value.Length > MaxNameLength ? value[..MaxNameLength] : value;

    #endregion
}
