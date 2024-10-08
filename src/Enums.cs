using System.Text.Json.Serialization;

namespace Slap;

[Newtonsoft.Json.JsonConverter(typeof(JsonStringEnumConverter))]
public enum BrowserType
{
    HttpClient,
    Chromium,
    Firefox,
    Webkit
}

public enum EntryType
{
    Asset,
    External,
    HtmlDocument
}