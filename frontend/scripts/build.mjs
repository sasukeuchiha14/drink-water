import { copyFile, mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { createHash } from "node:crypto";

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
  "hi/index.html",
  "policies/index.html",
  "styles.css",
  "main.js",
  "theme.js",
  "preview-timer.js",
  "slogan-preview.js",
  "404.html",
  "robots.txt",
  "sitemap.xml",
]) {
  await mkdir(dirname(join(destination, name)), { recursive: true });
  await copyFile(join(frontendRoot, "src", name), join(destination, name));
}
await copyFile(
  join(projectRoot, "logo.ico"),
  join(destination, "assets", "logo.ico"),
);
await copyFile(join(projectRoot, "LICENSE"), join(destination, "LICENSE.txt"));
for (const name of ["google0a09052122f3286f.html", "social-card.png"]) {
  await copyFile(join(frontendRoot, "public", name), join(destination, name));
}
const dataHashes = [];
for (const page of ["index.html", "hi/index.html"]) {
  const html = (await readFile(join(destination, page), "utf8")).replaceAll(
    "__APP_VERSION__",
    version,
  );
  if (!html.includes("releases/latest/download/DrinkWater-Setup.exe")) {
    throw new Error(`The production download link is missing from ${page}.`);
  }
  const structuredData = html.match(
    /<script type="application\/ld\+json">([\s\S]*?)<\/script>/,
  )?.[1];
  if (!structuredData)
    throw new Error(`Structured data is missing from ${page}.`);
  JSON.parse(structuredData);
  dataHashes.push(
    `'sha256-${createHash("sha256").update(structuredData).digest("base64")}'`,
  );
  await writeFile(join(destination, page), html);
}
const headerTemplate = await readFile(
  join(frontendRoot, "src", "_headers"),
  "utf8",
);
await writeFile(
  join(destination, "_headers"),
  headerTemplate.replaceAll("__STRUCTURED_DATA_HASH__", dataHashes.join(" ")),
);
await writeFile(
  join(destination, "_redirects"),
  [
    "/google0a09052122f3286f.html /google0a09052122f3286f.html 200!",
    "/api/update /.netlify/functions/update 200!",
    "/api/version /.netlify/functions/update 200!",
    "https://drink-water.hardikgarg.me/* https://drinkwater.hardikgarg.me/:splat 301!",
    "/download https://github.com/sasukeuchiha14/drink-water/releases/latest/download/DrinkWater-Setup.exe 302",
    "",
  ].join("\n"),
);
console.log("Static website built in frontend/dist.");
