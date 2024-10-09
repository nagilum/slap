using System.Text.Json.Serialization;

namespace Slap;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BrowserType
{
    HttpClient,
    Chromium,
    Firefox,
    Webkit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntryType
{
    Asset,
    External,
    HtmlDocument
}