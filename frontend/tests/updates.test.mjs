import test from "node:test";
import assert from "node:assert/strict";
import {
  compareVersions,
  createReleaseLoader,
  createUpdateHandler,
  parseVersion,
  releaseMetadata,
} from "../lib/updates.mjs";

function release(version = "1.10.0") {
  return {
    tag_name: "v" + version,
    draft: false,
    prerelease: false,
    assets: [
      {
        name: "DrinkWater-Setup.exe",
        state: "uploaded",
        size: 1024,
        browser_download_url: `https://github.com/sasukeuchiha14/drink-water/releases/download/v${version}/DrinkWater-Setup.exe`,
      },
    ],
  };
}

test("versions compare numbers rather than strings, with no downgrade", () => {
  assert.equal(compareVersions("1.10.0", "1.9.9"), 1);
  assert.equal(compareVersions("2.0.0", "1.99.99"), 1);
  assert.equal(compareVersions("1.0.0", "1.0.1"), -1);
  assert.equal(compareVersions("1.1.0", "1.1.0"), 0);
});

test("version input rejects ambiguous, prerelease, overflowing and injected values", () => {
  for (const version of [
    "1.0",
    "1.0.0.0",
    "01.0.0",
    "v1.0.0",
    "1.0.0-beta",
    "-1.0.0",
    "2147483648.0.0",
    "1.0.0\n",
    "<script>",
    "",
  ]) {
    assert.equal(parseVersion(version), null, version);
  }
});

test("only stable uploaded installers from this repository are advertised", () => {
  for (const update of [
    { draft: true },
    { prerelease: true },
    { tag_name: "v1.0.0-beta" },
    { assets: [] },
    {
      assets: [
        {
          ...release().assets[0],
          browser_download_url: "https://evil.example/setup.exe",
        },
      ],
    },
    { assets: [{ ...release().assets[0], state: "new" }] },
  ]) {
    assert.throws(() => releaseMetadata({ ...release(), ...update }));
  }
  assert.equal(releaseMetadata(release()).latestVersion, "1.10.0");
});

test("both hostnames return comparison results and metadata", async () => {
  const handler = createUpdateHandler(async () => releaseMetadata(release()));
  for (const host of [
    "drinkwater.hardikgarg.me",
    "drink-water.hardikgarg.me",
  ]) {
    for (const [version, expected] of [
      ["1.9.0", true],
      ["1.10.0", false],
      ["2.0.0", false],
    ]) {
      const response = await handler(
        new Request(`https://${host}/api/update?version=${version}`),
      );
      assert.equal(response.status, 200);
      assert.equal(response.headers.get("cache-control"), "no-store");
      const body = await response.json();
      assert.equal(body.currentVersion, version);
      assert.equal(body.updateAvailable, expected);
    }
    const metadata = await handler(new Request(`https://${host}/api/version`));
    assert.equal((await metadata.json()).latestVersion, "1.10.0");
  }
});

test("invalid requests never contact GitHub; upstream failures return a safe retry response", async () => {
  let calls = 0;
  const handler = createUpdateHandler(async () => {
    calls++;
    throw new Error("private upstream details");
  });
  assert.equal(
    (await handler(new Request("https://example.com/api/update?version=bad")))
      .status,
    400,
  );
  assert.equal(
    (
      await handler(
        new Request(
          "https://example.com/api/update?version=1.0.0&version=2.0.0",
        ),
      )
    ).status,
    400,
  );
  assert.equal(
    (
      await handler(
        new Request("https://example.com/api/update", { method: "POST" }),
      )
    ).status,
    405,
  );
  assert.equal(calls, 0);
  const failure = await handler(
    new Request("https://example.com/api/update?version=1.0.0"),
  );
  assert.equal(failure.status, 503);
  assert.equal(failure.headers.get("retry-after"), "30");
  assert.equal((await failure.text()).includes("private"), false);
});

test("GitHub requests are shared, cached, and refreshed after five minutes", async () => {
  let calls = 0;
  let time = 0;
  const load = createReleaseLoader({
    now: () => time,
    fetchImpl: async (url) => {
      assert.equal(
        url,
        "https://api.github.com/repos/sasukeuchiha14/drink-water/releases/latest",
      );
      calls++;
      return Response.json(release());
    },
  });
  await Promise.all([load(), load(), load()]);
  await load();
  assert.equal(calls, 1);
  time += 300001;
  await load();
  assert.equal(calls, 2);
});

test("upstream failures use a cooldown and oversized metadata is rejected", async () => {
  let calls = 0;
  let time = 0;
  const load = createReleaseLoader({
    now: () => time,
    fetchImpl: () => {
      calls++;
      throw new Error("offline");
    },
  });
  await assert.rejects(load());
  await assert.rejects(load());
  assert.equal(calls, 1);
  time = 30001;
  await assert.rejects(load());
  assert.equal(calls, 2);
  const oversized = createReleaseLoader({
    fetchImpl: async () =>
      new Response("{}", {
        headers: {
          "content-type": "application/json",
          "content-length": "1048577",
        },
      }),
  });
  await assert.rejects(oversized(), /size limit/);
});
