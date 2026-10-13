# Drink Water download website

A static HTML/CSS/JavaScript site with one Netlify update function. No production JavaScript dependencies,
third-party scripts, cookies, or tracking. The interactive tray preview runs only
in the browser and does not change Windows settings.

The icon is copied from the repository's `logo.ico` during the build.
Installer downloads come from the public GitHub release, keeping large binaries
out of Git and out of Netlify deployments.

## Local preview

Use Node.js 22 or later:

```powershell
npm run check
npm run build
npm run preview
```

Open `http://127.0.0.1:4173`. Generated files live in `dist/`. The preview server
also runs the update handler locally; update requests require GitHub connectivity.

## Deploy on Netlify

1. Import `sasukeuchiha14/drink-water` from GitHub and select the `main` branch.
2. The root `netlify.toml` supplies the settings: base directory `frontend`,
   build command `npm run check && npm run build`, publish directory `dist`,
   and Node.js 24.
3. In Domain management, add `drinkwater.hardikgarg.me` as the primary domain
   and `drink-water.hardikgarg.me` as an alias.
4. If DNS is managed outside Netlify, point each subdomain's CNAME record at
   your assigned `<site-name>.netlify.app` address. Follow Netlify's DNS checks
   and enable HTTPS after verification.

The hyphenated alias redirects website pages to the primary domain, preserving
the requested path. API rewrites run before that redirect, so update checks can
use either host. Both names must be added to this Netlify site for routing and TLS.
The primary domain also appears in the canonical URL and sitemap.

See [Netlify build configuration](https://docs.netlify.com/build/configure-builds/file-based-configuration/)
and [domain aliases](https://docs.netlify.com/manage/domains/configure-domains/add-a-domain-alias/).

## Downloads and releases

The buttons link to:

`https://github.com/sasukeuchiha14/drink-water/releases/latest/download/DrinkWater-Setup.exe`

GitHub's latest-release route selects the newest published release. The release
workflow always uses this asset name and uploads `SHA256SUMS.txt` alongside it.
The Netlify `/download` route redirects to the same asset.

For a new release, update the Windows version and release notes, then push a
matching version tag. The website's displayed version is read from the Windows
project at build time.

## Update endpoint

Netlify deploys `netlify/functions/update.mjs` alongside the static site. Deploy
from this repository with the root configuration; uploading only `dist/` does
not deploy the API. No API key or environment variable is required.

Both domains support:

```text
GET /api/version
GET /api/update?version=1.0.0
```

The first returns the latest published stable release. The second also compares
the caller's installed version numerically:

```json
{
  "schemaVersion": 1,
  "latestVersion": "1.1.0",
  "releaseUrl": "https://github.com/sasukeuchiha14/drink-water/releases/tag/v1.1.0",
  "downloadUrl": "https://github.com/sasukeuchiha14/drink-water/releases/download/v1.1.0/DrinkWater-Setup.exe",
  "currentVersion": "1.0.0",
  "updateAvailable": true
}
```

Equal or newer installed versions return `updateAvailable: false`. Versions use
three nonnegative integer components without leading zeros; previews and invalid
or duplicate version parameters return HTTP 400. Unsupported methods return 405.
GitHub failures or missing release assets return 503 with `Retry-After: 30`.
Comparison responses are not cached. Each warm function instance caches verified
GitHub metadata for five minutes and shares concurrent requests. Only uploaded
installers belonging to this repository are advertised.

The app tries `drinkwater.hardikgarg.me` first, then `drink-water.hardikgarg.me`.
It makes requests only when the user chooses **Check for updates**. Choosing
**Download version** opens the trusted GitHub installer URL in the browser;
the user runs the installer to upgrade. There is no automatic installer execution.

## Website theme

The header theme button switches between light and dark. The initial theme
follows the operating system; an explicit choice is saved in browser local
storage and restored on later visits. Blocked storage falls back to a session
choice. This preference does not change the Windows app's appearance.

## Accessibility and hosting

The page includes a skip link, keyboard focus styles, native FAQ controls,
labelled preview controls, responsive layouts, and reduced-motion support.
Netlify configuration sets a Content Security Policy, content-type protection,
frame restrictions, and restrictive browser permissions.

Deployment previews use the same static files. DNS and Netlify account
configuration are completed in your own hosting account.
