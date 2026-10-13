# Drink Water download website

A static HTML/CSS/JavaScript site. No production JavaScript dependencies,
third-party scripts, cookies, or tracking. The interactive tray preview runs only
in the browser and does not change Windows settings.

The icon is copied from the repository's original `logo.ico` during the build.
Installer downloads come from the public GitHub release, keeping large binaries
out of Git and out of Netlify deployments.

## Local preview

Use Node.js 22 or later:

```powershell
npm run check
npm run build
npm run preview
```

Open `http://127.0.0.1:4173`. Generated files live in `dist/`.

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

The hyphenated alias redirects to the primary domain, preserving the requested
path. Both names must be added to this Netlify site for routing and TLS to work.
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

## Accessibility and hosting

The page includes a skip link, keyboard focus styles, native FAQ controls,
labelled preview controls, responsive layouts, and reduced-motion support.
Netlify configuration sets a Content Security Policy, content-type protection,
frame restrictions, and restrictive browser permissions.

Deployment previews use the same static files. DNS and Netlify account
configuration are completed in your own hosting account.
