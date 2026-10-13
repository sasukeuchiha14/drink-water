import { PreviewTimer, formatCountdown } from "./preview-timer.js";

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
  notification.hidden = false;
  clearTimeout(notificationTimeout);
  notificationTimeout = setTimeout(() => {
    notification.hidden = true;
  }, 4500);
}

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
