# Website

[drinkwater.hardikgarg.me](https://drinkwater.hardikgarg.me/)

## Development

Requires Node.js 22 or later. From this directory:

```powershell
npm run check
npm run build
npm run preview
```

Build output is in `dist/`; local preview runs at `http://127.0.0.1:4173`.

The update API is available at `/api/version` and `/api/update?version=1.1.0`. It reports the latest published Windows release and compares stable versions numerically.
