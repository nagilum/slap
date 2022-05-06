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
slap <url> [<options>]

Options:

 -t <milliseconds>   Timeout to use for each request. Pass 0 to disable timeout.
 -r <url>            Set the referer for each request. If used with the -rp param, this will only be used for the first request.
 -rp                 Enable to set referer for each request to the parent the link was found on.
 -p <path>           Set the report path. Defaults to working directory.
 -ff                 Set Firefox as the rendering engine. Defaults to Chromium.
 -wk                 Set Webkit as the rendering engine. Defaults to Chromium.
 -vh <header>        Verify that a header exists.
 -vh <header:value>  Verify that a header and value exists. Value can be regex.
 -wu <state>         When to consider the request operation succeeded. Defaults to 'load'. Possible states are:
                      * domcontentloaded - When the DOMContentLoaded event is fired.
                      * load - When the load event is fired.
                      * networkidle - When there are no network connections for at least 500 ms.
                      * commit - When network response is received and the document started loading.
```

## Examples

This will scan https://microsoft.com/ and continue scanning all URLs found on that and proceeding pages.

```
$ slap https://microsoft.com/
```

This will scan https://microsoft.com/ using the Firefox engine, it will set the origin page as referer for each request, and it will wait for the DOMContentLoaded event to consider a request successful.

```
$ slap https://microsoft.com/ -ff -rp -wu domcontentloaded
```