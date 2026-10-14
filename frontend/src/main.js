import { PreviewTimer, formatCountdown } from "./preview-timer.js";
import { parseSlogans, SloganPreview } from "./slogan-preview.js";

const timer = new PreviewTimer();
const countdown = document.querySelector("#preview-countdown");
const countdownLabel = document.querySelector("#preview-countdown-label");
const enabled = document.querySelector("#preview-enabled");
const sound = document.querySelector("#preview-sound");
const startup = document.querySelector("#preview-startup");
const remindNow = document.querySelector("#preview-remind");
const notification = document.querySelector("#preview-notification");
const intervalButtons = document.querySelectorAll("[data-interval]");
const previewStatus = document.querySelector("#preview-status");
const customInterval = document.querySelector("#preview-custom-interval");
const customApply = document.querySelector("#preview-custom-apply");
const customError = document.querySelector("#preview-custom-error");
const randomSlogans = document.querySelector("#preview-random-slogans");
const sloganInput = document.querySelector("#preview-slogans");
const sloganError = document.querySelector("#preview-slogan-error");
const sloganPreview = new SloganPreview();
let messages = parseSlogans(sloganInput.value);
let notificationTimeout;

function render() {
  countdown.textContent = formatCountdown(timer.remainingSeconds());
  countdownLabel.textContent = timer.enabled
    ? "Next reminder in"
    : "Reminders disabled";
  countdown.classList.toggle("is-paused", !timer.enabled);
  remindNow.disabled = !timer.enabled;
  for (const button of intervalButtons) {
    button.setAttribute(
      "aria-pressed",
      String(Number(button.dataset.interval) === timer.minutes),
    );
  }
}

function showReminder() {
  notification.querySelector("p").textContent = sloganPreview.next(
    messages,
    randomSlogans.checked,
  );
  notification.hidden = false;
  clearTimeout(notificationTimeout);
  notificationTimeout = setTimeout(() => {
    notification.hidden = true;
  }, 4500);
}

function applyCustomInterval() {
  const minutes = Number(customInterval.value);
  if (
    !/^\d+$/.test(customInterval.value) ||
    !Number.isInteger(minutes) ||
    minutes < 1 ||
    minutes > 1440
  ) {
    customInterval.setAttribute("aria-invalid", "true");
    customError.hidden = false;
    return;
  }
  customInterval.removeAttribute("aria-invalid");
  customError.hidden = true;
  timer.setInterval(minutes);
  previewStatus.textContent =
    "Custom preview interval set to " + minutes + " minutes.";
  render();
}
customApply.addEventListener("click", applyCustomInterval);
customInterval.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    event.preventDefault();
    applyCustomInterval();
  }
});
sloganInput.addEventListener("input", () => {
  try {
    messages = parseSlogans(sloganInput.value);
    sloganInput.removeAttribute("aria-invalid");
    sloganError.hidden = true;
  } catch {
    sloganInput.setAttribute("aria-invalid", "true");
    sloganError.hidden = false;
  }
});

for (const button of intervalButtons) {
  button.addEventListener("click", () => {
    timer.setInterval(Number(button.dataset.interval));
    previewStatus.textContent =
      "Preview interval set to " + timer.minutes + " minutes.";
    render();
  });
}

enabled.addEventListener("change", () => {
  timer.setEnabled(enabled.checked);
  if (!enabled.checked) {
    notification.hidden = true;
  }
  previewStatus.textContent = enabled.checked
    ? "Preview reminders enabled."
    : "Preview reminders paused.";
  render();
});

sound.addEventListener("change", () => {
  previewStatus.textContent = sound.checked
    ? "Water sound enabled in the preview."
    : "Water sound disabled in the preview.";
});

startup.addEventListener("change", () => {
  previewStatus.textContent = startup.checked
    ? "Startup enabled in the preview."
    : "Startup disabled in the preview.";
});

remindNow.addEventListener("click", () => {
  timer.restart();
  showReminder();
  render();
});

render();
setInterval(() => {
  if (timer.tick()) {
    showReminder();
  }
  render();
}, 1000);

document.addEventListener("visibilitychange", () => {
  if (document.visibilityState === "visible") {
    render();
  }
});
