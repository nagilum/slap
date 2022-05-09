# Slap

Slap a site and see what falls out. A simple command-line site check tool.

## Download and Build

```
$ git clone https://github.com/nagilum/slap
$ cd slap/src
$ dotnet build
```

## Usage

```
Usage:
 slap <url> [<options>]

Options:
 -t <milliseconds>   Timeout to use for each request. Pass 0 to disable timeout.
 -r <url>            Set the referer for each request. If used with the -rp param, this will only be used for the first request.
 -rp                 Enable to set referer for each request to the parent the link was found on.
 -p <path>           Set the report path. Defaults to working directory.
 -ff                 Set Firefox as the rendering engine. Defaults to Chromium.
 -wk                 Set Webkit as the rendering engine. Defaults to Chromium.
 -ua <agent>         Specify the user agent to use for all requests.
 -vh <header>        Verify that a header exists.
 -vh <header:value>  Verify that a header and value exists. Value can be regex.
 -rh <header:value>  Add request header and value.
 -hac <user:pwd>     Add HTTP authentication username and password credentials.
 -wht                Warn if HTML title tag is missing or empty.
 -whk                Warn if HTML meta keywords tag is missing or empty.
 -whd                Warn if HTML meta description tag is missing or empty.
 -bcsp               Bypass Content-Security-Policy.
 -wu <state>         When to consider the request operation succeeded. Defaults to 'load'. Possible states are:
                      * domcontentloaded - When the DOMContentLoaded event is fired.
                      * load - When the load event is fired.
                      * networkidle - When there are no network connections for at least 500 ms.
                      * commit - When network response is received and the document started loading.
```

## Examples

This will scan https://example.com/ and continue scanning all URLs found on that and proceeding pages.

```
$ slap https://example.com/
```

This will scan https://example.com/ using the Firefox engine, it will disable the timeout for each request, it will set the origin page as referer for each request, and it will wait for the DOMContentLoaded event to consider a request successful.

```
$ slap https://example.com/ -t 0 -ff -rp -wu domcontentloaded
```

## Planned Updates

* Figure out what the default request headers are and list them.
* Add option for setting device scale factor.
* Add option for setting screen-size.
* Add option for setting viewport-size.
* Add an image, either static or gif to show app in use.