using System.Text.Json.Serialization;

namespace Slap.Core;

public enum ErrorType
{
    RequestTimeout,
    Unhandled,
    UnresolvableHostname
}

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