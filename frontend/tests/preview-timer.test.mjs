import assert from "node:assert/strict";
import test from "node:test";
import { PreviewTimer, formatCountdown } from "../src/preview-timer.js";

test("intervals use complete deadlines and reject unsupported values", () => {
  let now = 0;
  const timer = new PreviewTimer(() => now);
  assert.equal(timer.remainingSeconds(), 1800);
  now = 60_000;
  timer.setInterval(30);
  assert.equal(timer.remainingSeconds(), 1740);
  timer.setInterval(10);
  assert.equal(timer.remainingSeconds(), 600);
  timer.setInterval(60);
  assert.equal(timer.remainingSeconds(), 3600);
  assert.throws(() => timer.setInterval(15), RangeError);
});

test("pause suppresses due reminders and enabling starts a fresh interval", () => {
  let now = 0;
  const timer = new PreviewTimer(() => now);
  timer.setEnabled(false);
  now = 10_000_000;
  assert.equal(timer.remainingSeconds(), null);
  assert.equal(timer.tick(), false);
  timer.setEnabled(true);
  assert.equal(timer.remainingSeconds(), 1800);
});

test("delayed browser callbacks produce one reminder", () => {
  let now = 0;
  const timer = new PreviewTimer(() => now);
  now = 10_000_000;
  assert.equal(timer.tick(), true);
  assert.equal(timer.tick(), false);
  assert.equal(timer.remainingSeconds(), 1800);
});

test("countdown includes hours and rounds up fractional seconds", () => {
  assert.equal(formatCountdown(null), "Paused");
  assert.equal(formatCountdown(3600), "1:00:00");
  assert.equal(formatCountdown(1800), "30:00");
  assert.equal(formatCountdown(0.1), "00:01");
  assert.equal(formatCountdown(-1), "00:00");
});
