import { copyFile, mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const projectRoot = resolve(frontendRoot, "..");
const destination = join(frontendRoot, "dist");
const projectProperties = await readFile(
  join(projectRoot, "windows", "Directory.Build.props"),
  "utf8",
);
const version = projectProperties.match(
  /<Version>(\d+\.\d+\.\d+)<\/Version>/,
)?.[1];
if (!version) {
  throw new Error("The Windows project needs a semantic application version.");
}
await mkdir(join(destination, "assets"), { recursive: true });
for (const name of [
  "index.html",
  "styles.css",
  "main.js",
  "theme.js",
  "preview-timer.js",
  "404.html",
  "robots.txt",
  "sitemap.xml",
]) {
  await copyFile(join(frontendRoot, "src", name), join(destination, name));
}
await copyFile(
  join(projectRoot, "logo.ico"),
  join(destination, "assets", "logo.ico"),
);
await copyFile(join(projectRoot, "LICENSE"), join(destination, "LICENSE.txt"));
const html = await readFile(join(destination, "index.html"), "utf8");
if (!html.includes("releases/latest/download/DrinkWater-Setup.exe")) {
  throw new Error("The production download link is missing.");
}
await writeFile(
  join(destination, "index.html"),
  html.replaceAll("__APP_VERSION__", version),
);
await writeFile(
  join(destination, "_redirects"),
  [
    "/api/update /.netlify/functions/update 200!",
    "/api/version /.netlify/functions/update 200!",
    "/download https://github.com/sasukeuchiha14/drink-water/releases/latest/download/DrinkWater-Setup.exe 302",
    "",
  ].join("\n"),
);
console.log("Static website built in frontend/dist.");
