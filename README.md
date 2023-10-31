# Slap

> Slap a site and see what falls out.

Slap is a simple CLI to assist with QA checking of a site. 

If you slap https://example.com it will crawl all URLs found, both internal and external, but not move beyond the initial domain. After it is done, it will generate a report.

![CLI Example](assets/cli-example.png?raw=true)

When Slap is finished running it will create a JSON of the queue as well as a HTML report, which will be stored in the report path, which can be set as a parameter or it defaults to the current directory.

Screenshots:

* [The report overview shows general stats, a list of all status codes returned, and a list of all requests made](assets/report-overview.png)
* [The details view for each request shows stats about the request, headers, meta tags, where it was linked from, and accessibility issues](assets/report-details.png)
* [The accessibility issues are listed by topic with each violation listed below with DOM selector and HTML snippet](assets/report-details-accessibility-issues.png)

## Download and Build

```bash
git clone https://github.com/nagilum/slap
cd slap/src
dotnet build
```

Slap is written in C#, [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0#runtime-7.0.13), and uses [Microsoft Playwright](https://www.nuget.org/packages/Microsoft.Playwright) to perform the internal webpage requests. This allows us to take screenshots run [Axe Core](https://www.nuget.org/packages/Deque.AxeCore.Playwright) accessibility scans. [Serilog](https://www.nuget.org/packages/Serilog) is used for logging during run.

## How To Run

```bash
slap https://example.com --engine firefox
```

## Command Line Arguments

* [Set Rendering Engine](#set-rendering-engine)
* [Add Internal Domain](#add-internal-domain)
* [Set Report Path](#set-report-path)
* [Skip Scanning of Links](#skip-scanning-of-links)
* [Set Timeout](#set-timeout)
* [Set Window Size](#set-window-size)
* [Load Queue File](#load-queue-file)

### Set Rendering Engine

Set which rendering engine to use.

```
--engine <name-of-engine>
```

Valid options are:

* `chromium`
* `firefox`
* `webkit`

The default value is `chromium`.

#### Example

```
slap https://example.com --engine firefox
```

This will set the rendering engine to `Firefox`.


### Add Internal Domain

Add a domain to be treated as an internal domain.

```
--add <domain>
```

#### Example

```
slap https://example.com --add docs.example.com
```

This will follow links on both `example.com` and `docs.example.com`.


### Set Report Path

Set the folder path to save the report after scanning. It defaults to the current directory.

```
--path <path>
```

If the path does not exist, it will be created.

#### Example

```
slap https://example.com --path ~/slap-reports/
```

This will set the report path to `~/slap-reports/`.

### Skip Scanning of Links

Set whick link types to skip. This command can be repeated to set more skips.

Valid options are:

* `assets` - Will skip both internal and external assets.
* `external` - Will skip external assets and webpages.
* `external-assets` - Will skip external assets.
* `external-webpages` - Will skip external webpages.
* `internal-assets` - Will skip internal assets.

#### Example

```
slap https://example.com --skip external --skip internal-assets
```

This will skip internal assets, external assets, and external webpages.

### Skip Scanning of Domains

Set a domain to be skipped. This command can be repeated to set more domains.

#### Example

```
slap https://example.com --skip www.iana.org
```

This will scan the example.com domain, but skip scanning all links and assets found on www.iana.org.

### Set Timeout

Set the request timeout, in seconds, for each request. This setting defaults to 10 seconds.

#### Example

```
slap https://example.com --timeout 2
```

This will set the timeout for all request to 2 seconds.

### Save Screenshots

Set to save screenshots of each internal page that is scanned.

```
--screenshots
```

#### Example

```
slap https://example.com --screenshots
```

This will tell the program to save a screenshot for each internal page that's scanned.

### Set Window Size

Set the window size. This will affect the size of the screenshot as well as some accessibility checks.

#### Example

```
slap https://example.com --size 1024x768
```

This will set the window size to 1024x768 px.

### Load Queue File

This will load the specified queue file instead of crwling from first URL.
Slap will then re-scan all the entries that failed in any way on the previous run.

#### Example

```
slap --load ./reports/example.com/queue.json
```

This will load the specified queue file and re-scan based on it.