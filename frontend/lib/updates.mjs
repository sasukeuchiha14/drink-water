const repository = "https://github.com/sasukeuchiha14/drink-water";
const latestReleaseApi =
  "https://api.github.com/repos/sasukeuchiha14/drink-water/releases/latest";
const maximumVersionPart = 2147483647;

export function parseVersion(text) {
  if (
    typeof text !== "string" ||
    !/^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(text)
  ) {
    return null;
  }
  const parts = text.split(".").map(Number);
  return parts.every((part) => part <= maximumVersionPart) ? parts : null;
}

export function compareVersions(left, right) {
  const a = parseVersion(left);
  const b = parseVersion(right);
  if (!a || !b) throw new TypeError("Use a stable major.minor.patch version.");
  for (let index = 0; index < 3; index++) {
    if (a[index] !== b[index]) return Math.sign(a[index] - b[index]);
  }
  return 0;
}

export function releaseMetadata(release) {
  const latestVersion = release?.tag_name?.startsWith("v")
    ? release.tag_name.slice(1)
    : null;
  if (
    !parseVersion(latestVersion) ||
    release.draft !== false ||
    release.prerelease !== false
  ) {
    throw new Error("The latest release is not a published stable version.");
  }
  const downloadUrl = `${repository}/releases/download/v${latestVersion}/DrinkWater-Setup.exe`;
  const installer = release.assets?.find(
    (asset) => asset.name === "DrinkWater-Setup.exe",
  );
  if (
    installer?.state !== "uploaded" ||
    !Number.isSafeInteger(installer.size) ||
    installer.size <= 0 ||
    installer.browser_download_url !== downloadUrl
  ) {
    throw new Error("The published installer is unavailable.");
  }
  return {
    schemaVersion: 1,
    latestVersion,
    releaseUrl: `${repository}/releases/tag/v${latestVersion}`,
    downloadUrl,
  };
}

export function createReleaseLoader({
  fetchImpl = fetch,
  now = Date.now,
} = {}) {
  let cached;
  let expiresAt = 0;
  let retryAfter = 0;
  let pending;
  return async function loadRelease() {
    if (cached && now() < expiresAt) return cached;
    if (pending) return pending;
    if (now() < retryAfter)
      throw new Error("The release service is temporarily unavailable.");
    pending = (async () => {
      try {
        const response = await fetchImpl(latestReleaseApi, {
          headers: {
            Accept: "application/vnd.github+json",
            "User-Agent": "DrinkWater-UpdateEndpoint",
            "X-GitHub-Api-Version": "2022-11-28",
          },
          signal: AbortSignal.timeout(8000),
          redirect: "error",
        });
        if (
          !response.ok ||
          !response.headers.get("content-type")?.includes("application/json")
        ) {
          throw new Error("GitHub could not provide release metadata.");
        }
        const maximumBytes = 1024 * 1024;
        if (Number(response.headers.get("content-length")) > maximumBytes) {
          throw new Error("Release metadata exceeded the size limit.");
        }
        const reader = response.body.getReader();
        const chunks = [];
        let length = 0;
        try {
          while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            length += value.byteLength;
            if (length > maximumBytes)
              throw new Error("Release metadata exceeded the size limit.");
            chunks.push(value);
          }
        } finally {
          await reader.cancel();
          reader.releaseLock();
        }
        const bytes = new Uint8Array(length);
        let offset = 0;
        for (const chunk of chunks) {
          bytes.set(chunk, offset);
          offset += chunk.byteLength;
        }
        cached = releaseMetadata(JSON.parse(new TextDecoder().decode(bytes)));
        expiresAt = now() + 5 * 60 * 1000;
        return cached;
      } catch (error) {
        retryAfter = now() + 30 * 1000;
        throw error;
      }
    })().finally(() => {
      pending = undefined;
    });
    return pending;
  };
}

export function createUpdateHandler(loadRelease = createReleaseLoader()) {
  return async function update(request) {
    const headers = {
      "Content-Type": "application/json; charset=utf-8",
      "Cache-Control": "no-store",
      "X-Content-Type-Options": "nosniff",
      "X-Robots-Tag": "noindex",
      "X-Frame-Options": "DENY",
      "Referrer-Policy": "no-referrer",
      "Content-Security-Policy": "default-src 'none'; frame-ancestors 'none'",
    };
    const respond = (body, status = 200) =>
      new Response(JSON.stringify(body), { status, headers });
    if (request.method !== "GET" && request.method !== "HEAD") {
      headers.Allow = "GET, HEAD";
      return respond({ error: "Method not allowed." }, 405);
    }
    const versions = new URL(request.url).searchParams.getAll("version");
    if (
      versions.length > 1 ||
      (versions.length === 1 && !parseVersion(versions[0]))
    ) {
      return respond(
        { error: "Use version=major.minor.patch, for example version=1.0.0." },
        400,
      );
    }
    try {
      const metadata = await loadRelease();
      const result =
        versions.length === 1
          ? {
              ...metadata,
              currentVersion: versions[0],
              updateAvailable:
                compareVersions(metadata.latestVersion, versions[0]) > 0,
            }
          : metadata;
      return request.method === "HEAD"
        ? new Response(null, { headers })
        : respond(result);
    } catch {
      headers["Retry-After"] = "30";
      return respond(
        {
          error: "Updates are temporarily unavailable. Please try again later.",
        },
        503,
      );
    }
  };
}
