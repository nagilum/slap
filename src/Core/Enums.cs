using System.Text.Json.Serialization;

namespace Slap.Core;

public enum LogLevel
{
    Silent,
    Normal,
    Verbose
}

public enum RenderingEngine
{
    Chromium,
    Firefox,
    Webkit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UrlType
{
    InternalPage,
    InternalAsset,
    ExternalPage,
    ExternalAsset
}