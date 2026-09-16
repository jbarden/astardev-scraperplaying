# Playwright login persistence (Chrome, real profile)

How the scraper's Playwright browser session stays logged in to wallhaven.cc across runs, and
how to rebuild the setup from scratch if this machine gets wiped.

## The short version

The app drives a real, installed Google Chrome via Playwright's persistent-context launch,
pointed at a **dedicated** Chrome profile directory (not your everyday Chrome profile). You log
in to wallhaven.cc once, manually, in that same profile directory, using a Chrome launch command
that matches specific flags the app itself uses. After that, the app's browser session reuses
that profile and stays logged in.

Two things make this actually work, both required:

1. **`--disable-blink-features=AutomationControlled`** - suppresses Blink's automation flag
   (`navigator.webdriver`), which is a common signal sites use to detect and block automated
   browsers.
2. **`--password-store=basic`** - forces Chrome to use its own built-in, non-OS-keyring backend
   for encrypting saved cookies. This has to be used **consistently** on every launch, login
   included, or Chrome silently can't decrypt cookies written under a different backend (see
   "Why this is needed" below).

## Setup steps (do this once, or again after a wipe)

1. Make sure real, everyday Chrome is fully closed. Chrome locks its profile directory, so it
   can't be open elsewhere while a Playwright run (or your manual login) is using the same one.

2. Pick a dedicated profile directory - **do not use your everyday
   `~/.config/google-chrome` profile**. Using your real, Google-account-signed-in profile
   triggers Chrome's own "Verify that it's you" account-verification challenge when driven by
   automation, which is a separate problem from anything below and blocks the browser before it
   even reaches wallhaven.cc.

   This repo currently uses:
   ```
   /home/jbarden/.config/chrome-profile/
   ```

3. Launch Chrome manually against that directory, with the same flags the app uses:

   ```bash
   google-chrome --user-data-dir=/home/jbarden/.config/chrome-profile/ \
     --password-store=basic \
     --disable-blink-features=AutomationControlled
   ```

4. Log in to wallhaven.cc in that window normally.

5. Close that Chrome window completely (check `ps aux | grep chrome` shows nothing using that
   profile if in doubt).

6. Set `scraperAppConfiguration.userDataDirectory` in `AStarDev.ScraperPlaying/appsettings.json`
   to the same directory (see "Configuration" below). Then run the app - it attaches to that
   profile and should already be logged in.

If the session ever appears logged out again, redo steps 1-5 with a fresh login - always through
that same flag-matched Chrome launch command, never a plain `google-chrome` with no flags.

## Configuration

`AStarDev.ScraperPlaying/appsettings.json`:

```json
{
  "scraperAppConfiguration": {
    "userDataDirectory": "/home/jbarden/.config/chrome-profile/"
  }
}
```

Bound by `AStarDev.ScraperPlaying/Scraping/ScraperAppSettings.cs` (section name
`scraperAppConfiguration`), registered in `AStarDev.ScraperPlaying/Startup/ConfigurationServices.cs`
via `AddOptions<ScraperAppSettings>()`.

## The code

`AStarDev.ScraperPlaying/Scraping/PlaywrightBrowserSession.cs` launches the persistent context:

```csharp
context = await playwright.Chromium.LaunchPersistentContextAsync(settings.Value.UserDataDirectory, new BrowserTypeLaunchPersistentContextOptions
{
    Headless = useHeadless,
    Channel = "chrome",
    Args = ["--disable-blink-features=AutomationControlled", "--password-store=basic"],
    ViewportSize = new ViewportSize { Width = 2000, Height = 1200 },
    Locale = "en-GB",
    TimezoneId = "Europe/London",
});
```

Notes on the other options:

- `Channel = "chrome"` - uses the real installed Google Chrome binary, not Playwright's bundled
  Chromium build.
- `ViewportSize` / `Locale` / `TimezoneId` - set to plausible, consistent real-user values rather
  than Playwright's defaults, to avoid an obviously-automated fingerprint.

If this file ever needs rebuilding from nothing, that's the minimum config that's been proven to
work (matches a working implementation from an earlier version of this app).

## Why this is needed (in case it needs debugging again)

Confirmed by direct experiment on this machine (Fedora, KDE Plasma):

- Chrome's cookie storage encrypts cookie values using an OS-backed key. On this machine, Chrome's
  default backend goes through KDE's kwallet/freedesktop Secrets portal (a D-Bus service).
- A Chrome instance **launched by Playwright** (`LaunchPersistentContextAsync`, even using the
  real Chrome binary via `Channel = "chrome"`) could not complete that portal handshake. Result:
  `context.CookiesAsync()` reported **zero** cookies for wallhaven.cc, even though the profile's
  `Cookies` SQLite file on disk had real, valid session cookies (`wallhaven_session`,
  `cf_clearance`, etc.) from a manual login.
- A **plain, non-Playwright-launched** `google-chrome` process, with Playwright only attaching to
  it afterwards via CDP (`ConnectOverCDPAsync`), read the same cookies fine - confirming the
  failure was specific to Playwright's own launch path, not the profile or the cookies themselves.
- Passing `--password-store=basic` sidesteps the portal entirely, using Chrome's own built-in
  encryption instead. Verified directly: a cookie set by a Chrome launch using
  `--password-store=basic` was correctly readable by a **separate**, later Chrome launch that also
  used `--password-store=basic`. When the first launch used no flag (default/portal backend) and
  the second used `--password-store=basic`, this specific inconsistency didn't reproduce in an
  isolated test - but matching the flag on both sides is what a known-working prior implementation
  of this app did, and it's what fixed the real, non-synthetic case. **Always use the same
  `--password-store=basic` flag for every Chrome launch touching this profile - login included.**

## Dead ends (don't repeat these)

Things that were tried and ruled out, so you don't waste time re-treading them:

- **Cloudflare bot-detection fingerprinting** was the first suspect (a screenshot looked like it
  might be a "verify" challenge). Turned out not to be the actual blocker in practice - the page
  was loading fine, just logged out. Not worth chasing further unless cookies are confirmed
  present *and* readable and the page is still visibly challenged.
- **Attaching to an already-running Chrome via CDP** (`ConnectOverCDPAsync` against a separately
  user-launched Chrome with `--remote-debugging-port`) was tried as a Cloudflare workaround and
  reverted - it wasn't what previously worked on an earlier machine, and added real complexity
  (Chrome has to be pre-launched externally, disposal has to be careful never to close the user's
  real browser). Not currently used, but the code shape is in the git history (PR #154) if it's
  ever needed again.
- **Pointing `userDataDirectory` at your real, everyday, Google-signed-in Chrome profile**
  triggers Chrome's own "Verify that it's you" account check (unrelated to wallhaven or
  Cloudflare) and blocks navigation before the site even loads. Always use a dedicated profile.
