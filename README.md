# Slap

> Slap a site and see what falls out.

A simple command-line site check tool. Slap will start with the given URL and scan outwards till it has covered all links from the same domain/subdomain.

## Download and Build

```bash
git clone https://github.com/nagilum/slap
cd slap/src
dotnet build
```

## Command Line Arguments


### Set Rendering Engine

Set which rendering engine to use.

```
-re <name-of-engine>
```

Valid options are:

* `chromium`
* `firefox`
* `webkit`

The default value is `chromium`.

#### Example

```
slap https://example.com -re firefox
```

This will set the rendering engine to `Firefox`.


### Add Internal Domain

Add a domain to be treated as an internal domain.

```
-ad <domain>
```

#### Example

```
slap https://example.com -ad docs.example.com
```

This will follow links on both `example.com` and `docs.example.com`.


### Set Report Path

Set the folder path to save the report after scanning. It defaults to the current directory.

```
-rp <path>
```

If the path does not exist, it will be created.

#### Example

```
slap https://example.com -rp ~/slap-reports/
```

This will set the report path to `~/slap-reports/`.


### Load Playwright Config

Load custom config for the various Playwright objects.

```
-lc <path>
```

#### Example

```
slap https://example.com -lc ~/slap-config/default.json
```

This will load the Playwright config file from `~/slap-config/default.json`.


#### Config File Structure

```json
{
    "browserNewPageOptions": {},
    "browserTypeLaunchOptions": {},
    "pageGotoOptions": {}
}
```

The 3 objects are Playwright objects. Read more about them here:

* [BrowserNewPageOptions](https://www.fuget.org/packages/Microsoft.Playwright/1.14.0/lib/netstandard2.0/Microsoft.Playwright.dll/Microsoft.Playwright/BrowserNewPageOptions)
* [BrowserTypeLaunchOptions](https://www.fuget.org/packages/Microsoft.Playwright/1.14.0/lib/netstandard2.0/Microsoft.Playwright.dll/Microsoft.Playwright/BrowserTypeLaunchOptions)
* [PageGotoOptions](https://www.fuget.org/packages/Microsoft.Playwright/1.14.0/lib/netstandard2.0/Microsoft.Playwright.dll/Microsoft.Playwright/PageGotoOptions)


### Save Screenshots

Set to save screenshots of each internal page that is scanned.

```
-ss
```

The size of the screenshot can be defined by setting [ScreenSize](https://www.fuget.org/packages/Microsoft.Playwright/1.14.0/lib/netstandard2.0/Microsoft.Playwright.dll/Microsoft.Playwright/ScreenSize) in [BrowserNewPageOptions](https://www.fuget.org/packages/Microsoft.Playwright/1.14.0/lib/netstandard2.0/Microsoft.Playwright.dll/Microsoft.Playwright/BrowserNewPageOptions)

#### Example

```
slap https://example.com -ss
```

This will tell the program to save a screenshot for each internal page that's scanned.