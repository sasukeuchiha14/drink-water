import assert from "node:assert/strict";
import test from "node:test";
import { parseSlogans, SloganPreview } from "../src/slogan-preview.js";

test("slogan lists accept plain text, ignore comments, and remove duplicate messages", () => {
  assert.deepEqual(parseSlogans("# Notes\n\n Sip now. \nSip now.\n喝点水。"), [
    "Sip now.",
    "喝点水。",
  ]);
});
test("invalid slogan lists are rejected without arbitrary markup execution", () => {
  for (const text of [
    "",
    "# Only a comment",
    "x".repeat(201),
    "Bad\u0001control",
    Array.from({ length: 101 }, (_, i) => "Message " + i).join("\n"),
  ]) {
    assert.throws(() => parseSlogans(text), RangeError);
  }
  assert.deepEqual(parseSlogans("<script>alert(1)</script>"), [
    "<script>alert(1)</script>",
  ]);
});
test("random slogans avoid immediate repeats and fixed mode uses the first line", () => {
  const preview = new SloganPreview(() => 0);
  const messages = ["First", "Second", "Third"];
  assert.equal(preview.next(messages, true), "First");
  assert.equal(preview.next(messages, true), "Second");
  assert.equal(preview.next(messages, false), "First");
  assert.equal(preview.next(["Only"], true), "Only");
});
