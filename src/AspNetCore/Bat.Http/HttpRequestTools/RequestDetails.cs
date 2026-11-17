namespace Bat.Http;

public class RequestDetails
{
    public string OsName { get; set; }
    public string OsVersion { get; set; }
    public string BrowserName { get; set; }
    public string BrowserVersion { get; set; }
    public string Manufacture { get; set; }
    public string Model { get; set; }
    public string IP { get; set; }

    /// <summary><c>true</c> for phones, tablets and wearables.</summary>
    public bool IsMobile { get; set; }

    public bool IsTablet { get; set; }

    /// <summary>Crawler, link-preview fetcher or health check (Googlebot, facebookexternalhit, kube-probe, ...).</summary>
    public bool IsBot { get; set; }

    /// <summary>Desktop, Mobile, Tablet, TV, Console, Wearable, Bot or Unknown.</summary>
    public string DeviceType { get; set; }

    /// <summary>Blink, WebKit, Gecko, Trident, EdgeHTML, Presto, ... or Unknown.</summary>
    public string EngineName { get; set; }
    public string EngineVersion { get; set; }
}